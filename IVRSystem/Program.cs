using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using System.Net.WebSockets;
using System.Text.Json;
using System.Collections.Concurrent;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

#region
string accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
TwilioClient.Init(accountSid, authToken);

#endregion

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
});

// Connected WebSocket clients for audio/control
var wsClients = new ConcurrentDictionary<Guid, WebSocket>();

// Active calls tracked by CallSid
var activeCalls = new ConcurrentDictionary<string, string>();

// Twilio media streams for each call
var twilioStreams = new ConcurrentDictionary<string, WebSocket>();

//Websocket connections for each agent
var agentSockets = new ConcurrentDictionary<Guid, WebSocket>();

var callToStreamSid = new ConcurrentDictionary<string, string>();

var app = builder.Build();
app.UseForwardedHeaders();
app.UseWebSockets();

// Default route
app.MapGet("/", () => "Hello World.");

// DTMF server endpoint
app.MapPost("/dtmf", (HttpRequest request) =>
{
    var response = new VoiceResponse();
    // What information to gather from the user DTMF inputs
    var gather = new Gather(
        numDigits: 1,
        action: new Uri("/gather", UriKind.Relative),
        timeout: 30,
        method: "POST"
    );
    gather.Say("Enter a number on your keypad.");
    response.Append(gather); 
    response.Hangup();
    return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
});

// Voice call webhook
app.MapPost("/voice", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    string callSid = form["CallSid"];
    string fromNumber = form["From"];
    Console.WriteLine($"New voice call: {callSid}");

    // Store callSid so it can be redirected later
    activeCalls[callSid] = callSid;
    var incomingMsg = JsonSerializer.Serialize(new
    {
        @event = "IncomingCall",
        CallSid = callSid,
        From = fromNumber
    });

    foreach (var agent in agentSockets.Values)
    {
        if (agent.State == WebSocketState.Open)
        {
            await agent.SendAsync(
                Encoding.UTF8.GetBytes(incomingMsg),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
    var response = new VoiceResponse();
    var connect = new Twilio.TwiML.Voice.Connect();
    response.Say("Please wait while we connect you with an agent.");
    response.Pause(length: 30);
    connect.Stream(url: $"wss://{req.Host}/stream"); // Connect to audio/WebSocketMessage stream
    response.Append(connect);

    return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
});

// WebSocket endpoint for audio/WebSocketMessage
app.MapGet("/stream", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = Guid.NewGuid();
    string callSid = null;

    var buffer = new byte[16_384];
    var incomingData = new List<byte>();
    while (ws.State == WebSocketState.Open)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close) break;
        incomingData.AddRange(buffer.Take(result.Count));
        // Try to parse complete JSON from Twilio
        try
        {
            var jsonText = Encoding.UTF8.GetString(incomingData.ToArray());
            using var doc = JsonDocument.Parse(jsonText);
            string streamSid = null;
            if (doc.RootElement.TryGetProperty("event", out var evtProp))
            {
                var evt = evtProp.GetString();
                if (evt == "start")
                {
                    streamSid = doc.RootElement
                        .GetProperty("start")
                        .GetProperty("streamSid")
                        .GetString();
                    callSid = doc.RootElement
                        .GetProperty("start")
                        .GetProperty("callSid")
                        .GetString();
                    twilioStreams[callSid] = ws;
                    callToStreamSid[callSid] = streamSid;
                    Console.WriteLine($"Twilio stream started for {callSid}");
                }
                if (evt == "media")
                {
                    var media = doc.RootElement.GetProperty("media");
                    var payload = media.GetProperty("payload").GetString();
                    var track = media.GetProperty("track").GetString();

                    // Only process inbound audio from caller
                    if (track == "inbound")
                    {
                        var audioBytes = Convert.FromBase64String(payload);

                        // Forward to all connected WinForm clients
                        foreach (var agent in agentSockets.Values)
                        {
                            if (agent.State == WebSocketState.Open)
                            {
                                await agent.SendAsync(
                                    audioBytes,
                                    WebSocketMessageType.Binary,
                                    true,
                                    CancellationToken.None);
                            }
                        }
                    }
                    // Forward to all connected WinForm agents

                }
                else if (evt == "stop")
                {
                    Console.WriteLine("Twilio stream stopped");
                    Console.WriteLine($"Call ended: {callSid}");

                    // cleanup
                    twilioStreams.TryRemove(callSid, out _);
                    callToStreamSid.TryRemove(callSid, out _);
                    activeCalls.TryRemove(callSid, out _);

                    // notify all agents
                    var endMsg = JsonSerializer.Serialize(new
                    {
                        @event = "endCall",
                        CallSid = callSid
                    });

                    foreach (var agent in agentSockets.Values)
                    {
                        if (agent.State == WebSocketState.Open)
                        {
                            await agent.SendAsync(Encoding.UTF8.GetBytes(endMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }

                    break;
                }

            }
            incomingData.Clear(); // successfully parsed JSON

        }
        catch
        {
            // JSON incomplete, wait for next WebSocket frame
        }
    }
});

app.MapGet("/agent", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    var agentId = Guid.NewGuid();
    agentSockets[agentId] = ws;


    // Handle control messages from WinForm (redirectDTMF)

    var buffer = new byte[16_384];

    while (ws.State == WebSocketState.Open)
    {
        var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close) break;

        // Try to parse complete JSON from Twilio
        try
        {
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("payload", out var payloadProp))
            {
                var payload = payloadProp.GetString();
                var audio = Convert.FromBase64String(payload);
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                if (twilioStreams.TryGetValue(callSid, out var twiliows) && callToStreamSid.TryGetValue(callSid, out var streamSid))
                {
                    var twilioMsg = JsonSerializer.Serialize(
                        new
                        {
                            @event = "media",
                            streamSid = streamSid,
                            media = new
                            {
                                payload
                            }
                        });

                    await twiliows.SendAsync(Encoding.UTF8.GetBytes(twilioMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                }

            }

            if (doc.RootElement.TryGetProperty("Action", out var actionProp) && actionProp.GetString() == "redirectDTMF")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                Console.WriteLine($"Redirect request from WinForm for CallSid {callSid}");
                CallResource.Update(
                    pathSid: callSid,
                    url: new Uri($"https://uncoquettishly-bilgiest-bronson.ngrok-free.dev/dtmf"),
                    method: Twilio.Http.HttpMethod.Post
                );
                Console.WriteLine($"Call {callSid} redirected to /dtmf");
            }

            if (actionProp.GetString() == "acceptCall")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                Console.WriteLine($"agent {agentId} accepted call for CallSid {callSid}");
                CallResource.Update(
                    pathSid: callSid,
                    url: new Uri($"https://uncoquettishly-bilgiest-bronson.ngrok-free.dev/connect"),
                    method: Twilio.Http.HttpMethod.Post
                );
                var startMsg = JsonSerializer.Serialize(
                    new
                    {
                        @event = "start",
                        CallSid = callSid
                    });
                await ws.SendAsync(Encoding.UTF8.GetBytes(startMsg), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            if (actionProp.GetString() == "endCall")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                Console.WriteLine($"Agent requested endCall for {callSid}");

                // Hang up call via Twilio
                CallResource.Update(
                    pathSid: callSid,
                    status: CallResource.UpdateStatusEnum.Completed
                );

                // cleanup
                twilioStreams.TryRemove(callSid, out _);
                callToStreamSid.TryRemove(callSid, out _);
                activeCalls.TryRemove(callSid, out _);
            }
        }
        catch
        {
            // JSON incomplete, wait for next WebSocket frame
        }


    }
});

app.MapPost("/connect", (HttpRequest req) =>
    {
        var response = new VoiceResponse();
        var connect = new Connect();
        connect.Stream(url: $"wss://{req.Host}/stream");
        response.Append(connect);
        return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
    });

// Gather DTMF input from caller
app.MapPost("/gather", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var digit = form["Digits"].ToString(); // Digits entered to string
    Console.WriteLine($"DTMF received: {digit}");

    var response = new VoiceResponse();
    response.Say($"You pressed {digit}");
    response.Hangup();
    return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
});

app.Run();


// Define WebSocket control message
public class WebSocketMessage
{
    public string Action { get; set; }
    public string CallSid { get; set; }
}