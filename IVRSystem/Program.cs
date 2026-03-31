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
using Task = System.Threading.Tasks.Task;

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

// Track state of each call to determine how to interpret "stop" message from twilio
var callState = new ConcurrentDictionary<string, string>();

var callToAgent = new ConcurrentDictionary<string, Guid>();

var cardVerificationActive = new ConcurrentDictionary<string, bool>();

var cardDigitBuffer = new ConcurrentDictionary<string, StringBuilder>();


// Method to clean up calls that have ended
async Task BroadcastCallEnded(string callSid)
{
    var msg = JsonSerializer.Serialize(new
    {
        @event = "endCall",
        CallSid = callSid
    });

    foreach (var agent in agentSockets.Values)
    {
        if (agent.State == WebSocketState.Open)
        {
            await agent.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    twilioStreams.TryRemove(callSid, out _);
    callToStreamSid.TryRemove(callSid, out _);
    activeCalls.TryRemove(callSid, out _);
    callState.TryRemove(callSid, out _);
    callToAgent.TryRemove(callSid, out _);
    cardVerificationActive.TryRemove(callSid, out _);
    cardDigitBuffer.TryRemove(callSid, out _);
}

var app = builder.Build();
app.UseForwardedHeaders();
app.UseWebSockets();
app.UseStaticFiles();

// Default route
app.MapGet("/", () => "Hello World.");

// Hold endpoint
app.MapPost("/hold", (HttpRequest req) =>
{
    var response = new VoiceResponse();
    response.Play(new Uri($"https://{req.Host}/hold_quiet.mp3"), loop: 0);

    return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
});

app.MapPost("/status", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();

    var callSid = form["CallSid"].ToString();
    var status = form["CallStatus"].ToString();
    Console.WriteLine($"[STATUS] Call {callSid} -> {status}");

    if (status == "completed" || status == "canceled" || status == "no-answer")
    {
        Console.WriteLine($"Call ended ({status}): {callSid}");
        await BroadcastCallEnded(callSid);
    }
    return Results.Ok();
});

// DTMF server endpoint
app.MapPost("/dtmf", (HttpRequest request) =>
{
    var response = new VoiceResponse();
    // What information to gather from the user DTMF inputs
    var gather = new Gather(
        numDigits: 20,
        action: new Uri("/gather", UriKind.Relative),
        timeout: 120,
        finishOnKey: "#",
        method: "POST"
    );
    gather.Say("Enter payment card number into your keypad, followed by the pound sign.");
    response.Append(gather);
    response.Say("We did not receive your input. Please try again.");
    response.Redirect(new Uri("/dtmf", UriKind.Relative));
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
    callState[callSid] = "incoming";

    CallResource.Update(
    pathSid: callSid,
    statusCallback: new Uri($"https://{req.Host}/status"),
    statusCallbackMethod: Twilio.Http.HttpMethod.Post
    );
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
    response.Say("Please wait to be connected to an agent.");
    response.Pause(length: 60);

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
                        if (callSid != null &&
                            callToAgent.TryGetValue(callSid, out var assignedAgentId) &&
                            agentSockets.TryGetValue(assignedAgentId, out var assignedAgentWs) &&
                            assignedAgentWs.State == WebSocketState.Open)
                        {
                            await assignedAgentWs.SendAsync(
                                audioBytes,
                                WebSocketMessageType.Binary,
                                true,
                                CancellationToken.None);
                        }
                    }
                    // Forward to all connected WinForm agents

                }
                else if (evt == "dtmf")
                {
                    var digit = doc.RootElement.GetProperty("dtmf").GetProperty("digit").GetString();

                    if (!string.IsNullOrEmpty(callSid) &&
                        cardVerificationActive.TryGetValue(callSid, out bool isActive) &&
                        isActive)
                    {
                        if (!string.IsNullOrEmpty(digit))
                        {
                            Console.WriteLine($"DTMF for verification on {callSid}: {digit}");

                            var cardbuffer = cardDigitBuffer.GetOrAdd(callSid, _ => new StringBuilder());

                            if (digit == "#")
                            {
                                var cardNumber = cardbuffer.ToString();
                                cardbuffer.Clear();
                                cardVerificationActive[callSid] = false;

                                bool validLength = cardNumber.Length >= 13 && cardNumber.Length <= 19;
                                bool isValid = validLength && LuhnCheck(cardNumber);

                                if (callToAgent.TryGetValue(callSid, out var assignedAgent) &&
                                    agentSockets.TryGetValue(assignedAgent, out var agentWs) &&
                                    agentWs.State == WebSocketState.Open)
                                {
                                    var resultMsg = JsonSerializer.Serialize(new
                                    {
                                        @event = "cardVerificationResult",
                                        CallSid = callSid,
                                        Success = isValid
                                    });

                                    await agentWs.SendAsync(
                                        Encoding.UTF8.GetBytes(resultMsg),
                                        WebSocketMessageType.Text,
                                        true,
                                        CancellationToken.None);
                                }
                            }
                            else if (char.IsDigit(digit[0]))
                            {
                                cardbuffer.Append(digit);
                            }
                        }
                    }
                }
                else if (evt == "stop")
                {
                    Console.WriteLine($"Stream stopped for {callSid}");
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
                    method: Twilio.Http.HttpMethod.Post,
                    statusCallback: new Uri($"https://{context.Request.Host}/status"),
                    statusCallbackMethod: Twilio.Http.HttpMethod.Post
                );

                callState[callSid] = "redirecting";
                Console.WriteLine($"Call {callSid} redirected to /dtmf");
            }

            if (actionProp.GetString() == "acceptCall")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                Console.WriteLine($"agent {agentId} accepted call for CallSid {callSid}");
                CallResource.Update(
                    pathSid: callSid,
                    url: new Uri($"https://uncoquettishly-bilgiest-bronson.ngrok-free.dev/connect"),
                    method: Twilio.Http.HttpMethod.Post,
                    statusCallback: new Uri($"https://{context.Request.Host}/status"),
                    statusCallbackMethod: Twilio.Http.HttpMethod.Post
                );
                callState[callSid] = "connected";
                callToAgent[callSid] = agentId;
                cardVerificationActive[callSid] = false;
                cardDigitBuffer[callSid] = new StringBuilder();
                var startMsg = JsonSerializer.Serialize(
                    new
                    {
                        @event = "start",
                        CallSid = callSid
                    });
                await ws.SendAsync(Encoding.UTF8.GetBytes(startMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                var answeredMsg = JsonSerializer.Serialize(new
                {
                    @event = "callAnswered",
                    CallSid = callSid
                });

                // Notify all other agents that the call has been answered
                foreach (var agent in agentSockets)
                {
                    if (agent.Key != agentId && agent.Value.State == WebSocketState.Open)
                    {
                        await agent.Value.SendAsync(Encoding.UTF8.GetBytes(answeredMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }

            if (actionProp.GetString() == "startCardVerification")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();

                if (callToAgent.TryGetValue(callSid, out var assignedAgent) && assignedAgent == agentId)
                {
                    cardVerificationActive[callSid] = true;
                    cardDigitBuffer[callSid] = new StringBuilder();

                    Console.WriteLine($"Card verification started for call {callSid}");

                    var msg = JsonSerializer.Serialize(new
                    {
                        @event = "verificationStarted",
                        CallSid = callSid
                    });

                    await ws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }

            if (actionProp.GetString() == "putOnHold")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                Console.WriteLine($"Putting call {callSid} on hold");

                CallResource.Update(
                    pathSid: callSid,
                    url: new Uri($"https://{context.Request.Host}/hold"),
                    method: Twilio.Http.HttpMethod.Post,
                    statusCallback: new Uri($"https://{context.Request.Host}/status"),
                    statusCallbackMethod: Twilio.Http.HttpMethod.Post
                );

                callState[callSid] = "hold";
            }

            if (actionProp.GetString() == "takeOffHold")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                Console.WriteLine($"Taking call {callSid} off hold");

                CallResource.Update(
                    pathSid: callSid,
                    url: new Uri($"https://{context.Request.Host}/connect"),
                    method: Twilio.Http.HttpMethod.Post,
                    statusCallback: new Uri($"https://{context.Request.Host}/status"),
                    statusCallbackMethod: Twilio.Http.HttpMethod.Post
                );

                callState[callSid] = "connected";
            }

            if (actionProp.GetString() == "endCall")
            {
                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                Console.WriteLine($"Agent requested endCall for {callSid}");
                callState[callSid] = "agent_hangup";

                // Hang up call via Twilio
                CallResource.Update(
                    pathSid: callSid,
                    status: CallResource.UpdateStatusEnum.Completed
                );

                // cleanup
                twilioStreams.TryRemove(callSid, out _);
                callToStreamSid.TryRemove(callSid, out _);
                activeCalls.TryRemove(callSid, out _);
                callState.TryRemove(callSid, out _);
                callToAgent.TryRemove(callSid, out _);
                cardVerificationActive.TryRemove(callSid, out _);
                cardDigitBuffer.TryRemove(callSid, out _);
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
    var digits = form["Digits"].ToString(); // Digits entered to string

    var response = new VoiceResponse();

    if (digits.Length < 13 || digits.Length > 19)
    {
        response.Say("Invalid card number. Try again.");
        response.Redirect(new Uri("/dtmf", UriKind.Relative));
        return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
    }

    if (LuhnCheck(digits))
    {
        response.Say("Your card number was successfully validated.");
        response.Redirect(new Uri("https://uncoquettishly-bilgiest-bronson.ngrok-free.dev/connect"), Twilio.Http.HttpMethod.Post);
    }
    else
    {
        response.Say("Invalid card number. Please try again.");
        response.Redirect(new Uri("/dtmf", UriKind.Relative));
    }
    return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
});

 static bool LuhnCheck(string cardNumber)
{
    int sum = 0;
    bool alt = false;
    for (int i = cardNumber.Length - 1; i >= 0; i--)
    {
        int digit = cardNumber[i] - '0';
        if (alt)
        {
            digit *= 2;
            if (digit > 9)
            {
                digit -= 9;
            }
        }
        sum += digit;
        alt = !alt;
    }

    return (sum % 10 == 0);
}

app.Run();


// Define WebSocket control message
public class WebSocketMessage
{
    public string Action { get; set; }
    public string CallSid { get; set; }
}