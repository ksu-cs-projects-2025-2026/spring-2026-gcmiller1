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
string accountSid = "AC2bdf7af1ea3600cd900ddffde05b0875"; // don't do this
string authToken = "9c1e68efff8e9a5e612d3aa5a99b5325"; // don't do this either
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
    Console.WriteLine($"New voice call: {callSid}");

    // Store callSid so it can be redirected later
    activeCalls[callSid] = callSid;
    var startMsg = JsonSerializer.Serialize(new
    {
        Event = "callStarted",
        CallSid = callSid
    });

    foreach (var client in wsClients.Values)
    {
        if (client.State == WebSocketState.Open)
            await client.SendAsync(
                Encoding.UTF8.GetBytes(startMsg),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
    }
    var response = new VoiceResponse();
    var connect = new Twilio.TwiML.Voice.Connect();
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
    wsClients[clientId] = ws;

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

            if (doc.RootElement.TryGetProperty("event", out var evtProp))
            {
                var evt = evtProp.GetString();

                if (evt == "media")
                {
                    var media = doc.RootElement.GetProperty("media");
                    var payload = media.GetProperty("payload").GetString();
                    var track = media.GetProperty("track").GetString();

                    // Only process inbound audio from caller
                    if (track == "inbound")
                    {
                        var audioBytes = Encoding.UTF8.GetBytes(payload);

                        // Forward to all connected WinForm clients
                        foreach (var client in wsClients.Values)
                        {
                            if (client.State == WebSocketState.Open)
                                await client.SendAsync(audioBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
                else if (evt == "stop")
                {
                    Console.WriteLine("Twilio stream stopped");
                }
            }

            incomingData.Clear(); // successfully parsed JSON
        }
        catch
        {
            // JSON incomplete, wait for next WebSocket frame
        }

        // Handle control messages from WinForm (redirectDTMF)
        if (result.MessageType == WebSocketMessageType.Text)
        {
            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var ctrlMsg = JsonSerializer.Deserialize<WebSocketMessage>(text);

            if (ctrlMsg?.Action == "redirectDTMF" && !string.IsNullOrEmpty(ctrlMsg.CallSid))
            {
                Console.WriteLine($"Redirect request from WinForm for CallSid {ctrlMsg.CallSid}");
                if (activeCalls.TryGetValue(ctrlMsg.CallSid, out var callSid))
                {
                    CallResource.Update(
                        pathSid: callSid,
                        url: new Uri($"https://uncoquettishly-bilgiest-bronson.ngrok-free.dev/dtmf"),
                        method: Twilio.Http.HttpMethod.Post
                    );
                    Console.WriteLine($"Call {callSid} redirected to /dtmf");
                }
            }
        }
    }

    wsClients.TryRemove(clientId, out _);
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