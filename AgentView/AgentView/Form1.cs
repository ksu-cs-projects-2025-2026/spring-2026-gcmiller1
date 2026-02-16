using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Codecs;

namespace AgentView
{
    public partial class Form1 : Form
    {
        private ClientWebSocket ws;
        private WaveOutEvent waveOut;
        private BufferedWaveProvider bufferProvider;
        private WaveInEvent waveIn;
        private string currentCallSid;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await ConnectToIVRServerAsync();
        }

        /// <summary>
        /// Connects the WinForm application to the ASP.NET server
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToIVRServerAsync()
        {
            ws = new ClientWebSocket();

            try
            {
                await ws.ConnectAsync(
                    new Uri("wss://uncoquettishly-bilgiest-bronson.ngrok-free.dev/stream"), // media stream from the Twilio call
                    CancellationToken.None);

                // Setup audio playback
                waveOut = new WaveOutEvent();
                bufferProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1))
                {
                    BufferDuration = TimeSpan.FromSeconds(5),
                    DiscardOnBufferOverflow = true
                }; // Buffer for live audio
                waveOut.Init(bufferProvider);
                waveOut.Play();

                Console.WriteLine("Connected to IVR WebSocket server!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection failed: {ex}");
                return;
            }

            // Start listening for messages
            _ = Task.Run(async () =>
            {
                var buffer = new byte[8192]; // Audio buffer

                while (ws.State == WebSocketState.Open) // while websocket connection is live
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None); // Receive data from server

                    if (result.MessageType == WebSocketMessageType.Close) break; // If websocket message closes connection

                    string messageText = Encoding.UTF8.GetString(buffer, 0, result.Count); // Encodes received message
                    bool handled = false; // Checks if message has been handled yet

                    // parse JSON for control messages
                        using var doc = JsonDocument.Parse(messageText);

                        if (doc.RootElement.TryGetProperty("Event", out var evtProp))
                        {
                            var evt = evtProp.GetString();
                            if (evt == "callStarted")
                            {
                                currentCallSid = doc.RootElement.GetProperty("CallSid").GetString();
                                Console.WriteLine($"Current CallSid: {currentCallSid}");
                                handled = true;
                            }
                        }

                    if (!handled)
                    {
                        // Decode Twilio audio (base64 mu-law)
                        try
                        {
                            var muLawBytes = Convert.FromBase64String(messageText);
                            var pcmBuffer = new byte[muLawBytes.Length * 2];

                            // Converts u-law-encoded audio bytes to 16-bit PCM audio
                            for (int i = 0; i < muLawBytes.Length; i++)
                            {
                                short pcm = MuLawDecoder.MuLawToLinearSample(muLawBytes[i]);
                                pcmBuffer[i * 2] = (byte)(pcm & 0xff);
                                pcmBuffer[i * 2 + 1] = (byte)((pcm >> 8) & 0xff);
                            }

                            bufferProvider.AddSamples(pcmBuffer, 0, pcmBuffer.Length);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error decoding audio: {ex.Message}");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// When form is closed, close everything
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            waveIn?.StopRecording();
            waveOut?.Stop();
            ws?.Dispose();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// When SendToDTMF button is clicked, send message to server to redirect that call to the DTMF line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Btn_SendToDTMF_Click(object sender, EventArgs e)
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                MessageBox.Show("WebSocket not connected");
                return;
            }

            if (string.IsNullOrEmpty(currentCallSid))
            {
                MessageBox.Show("No CallSid available");
                return;
            }

            // Send message to server to redirect caller to DTMF
            var ctrlMsg = new
            {
                Action = "redirectDTMF",
                CallSid = currentCallSid
            };

            var json = JsonSerializer.Serialize(ctrlMsg);
            var bytes = Encoding.UTF8.GetBytes(json);

            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Sent redirectDTMF for CallSid {currentCallSid}");
        }
    }
}
