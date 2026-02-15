using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
});

var app = builder.Build();
app.UseForwardedHeaders();

// default path response
app.MapGet("/", () => "Hello World.");

// Requests user keypad input
app.MapPost("/voice", (HttpRequest request) =>
{
    var response = new VoiceResponse();
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

// gathers keypad inputs from user, says input back to user, hangs up
app.MapPost("/gather", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var digit = form["Digits"].ToString();
    Console.WriteLine($"DTMF received: {digit}");
    var response = new VoiceResponse();
    response.Say($"You pressed {digit}");
    response.Hangup();
    return Results.Text(response.ToString(), "text/xml", Encoding.UTF8);
});

app.Run();