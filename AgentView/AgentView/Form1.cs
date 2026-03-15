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
        private ClientWebSocket agentws;
        private WaveOutEvent waveOut;
        private BufferedWaveProvider bufferProvider;
        private WaveInEvent waveIn;
        private string currentCallSid;
        private Dictionary<string, IncomingCallControl> incomingCallRows = new();
        private AgentStatus Status;
        private OnCallControl activeCallControl;
        private bool micMuted = false;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            PopulateStatusDropdown();
        }

        private void PopulateStatusDropdown()
        {
            var statuses = Enum.GetValues(typeof(AgentStatus))
                .Cast<AgentStatus>()
                .Where(s => s != AgentStatus.OnCall)
                .ToList();

            comboBox1.DataSource = statuses;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await ConnectToIVRServerAsync();
        }

        private void SetAgentOnCall()
        {
            comboBox1.Enabled = false;
            comboBox1.Text = AgentStatus.OnCall.ToString();
            Status = AgentStatus.OnCall;
        }

        /// <summary>
        /// Connects the WinForm application to the ASP.NET server
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToIVRServerAsync()
        {

            try
            {
                // Setup audio playback
                waveOut = new WaveOutEvent();
                bufferProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1))
                {
                    BufferDuration = TimeSpan.FromSeconds(5),
                    DiscardOnBufferOverflow = true
                }; // Buffer for live audio
                waveOut.Init(bufferProvider);
                waveOut.Play();

                agentws = new ClientWebSocket();
                await agentws.ConnectAsync(
                    new Uri("wss://uncoquettishly-bilgiest-bronson.ngrok-free.dev/agent"), // media stream from agent mic to be sent back
                    CancellationToken.None);

                ReceiveCallerAudio();
                MicCapture();

                Console.WriteLine("Connected to IVR WebSocket server!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection failed: {ex}");
                return;
            }
        }

        private void ReceiveCallerAudio()
        {
            // Start listening for messages
            _ = Task.Run(async () =>
            {
                var buffer = new byte[8192]; // Audio buffer

                while (agentws.State == WebSocketState.Open) // while websocket connection is live
                {
                    var result = await agentws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None); // Receive data from server
                    if (result.MessageType == WebSocketMessageType.Close) break; // If websocket message closes connection

                    // If message received is Text, parse as json
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        using var doc = JsonDocument.Parse(json);

                        if (doc.RootElement.TryGetProperty("event", out var evtProp))
                        {
                            if (evtProp.GetString() == "start")
                            {
                                currentCallSid = doc.RootElement.GetProperty("CallSid").GetString();
                                Console.WriteLine($"Call started: {currentCallSid}");
                            }
                            if (evtProp.GetString() == "IncomingCall")
                            {
                                string callSid = doc.RootElement.GetProperty("CallSid").GetString();
                                string from = doc.RootElement.GetProperty("From").GetString();

                                this.Invoke(() =>
                                {
                                    AddIncomingCallUI(callSid, from);
                                });
                            }
                            if (evtProp.GetString() == "endCall")
                            {
                                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                                Console.WriteLine($"Call ended: {callSid}");

                                this.Invoke(() =>
                                {
                                    if (activeCallControl != null && activeCallControl.CallSid == callSid)
                                    {
                                        PanelIncomingCalls.Controls.Remove(activeCallControl);
                                        activeCallControl.Dispose();
                                        activeCallControl = null;
                                    }

                                    currentCallSid = "";
                                    SetAgentOffCall();
                                });
                            }
                        }
                    }
                    // If the message type received is Binary, it is audio and should be decoded as such
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        var muLawBytes = buffer.Take(result.Count).ToArray();
                        var pcmBuffer = new byte[muLawBytes.Length * 2];

                        for (int i = 0; i < muLawBytes.Length; i++)
                        {
                            short pcm = MuLawDecoder.MuLawToLinearSample(muLawBytes[i]);
                            pcmBuffer[i * 2] = (byte)(pcm & 0xff);
                            pcmBuffer[i * 2 + 1] = (byte)((pcm >> 8) & 0xff);
                        }

                        bufferProvider.AddSamples(pcmBuffer, 0, pcmBuffer.Length);
                    }
                }
            });
        }

        private async Task AcceptCall(string callSid, string fromNumber)
        {
            currentCallSid = callSid;

            var msg = JsonSerializer.Serialize(
                new
                {
                    Action = "acceptCall",
                    CallSid = callSid
                });

            await agentws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Accepted call {callSid}");
            SetAgentOnCall();

            var callCtrl = new OnCallControl(callSid, fromNumber)
            {
                Dock = DockStyle.Fill
            };
            callCtrl.MuteUnmute += ToggleMicMute;
            PanelIncomingCalls.Controls.Add(callCtrl);
            activeCallControl = callCtrl;
            callCtrl.CallEnded += async (_, __) =>
            {
                await EndCall(callSid);
                PanelIncomingCalls.Controls.Remove(callCtrl);
            };
        }

        private async Task EndCall(string callSid)
        {
            currentCallSid = callSid;

            var msg = JsonSerializer.Serialize(
                new
                {
                    Action = "endCall",
                    CallSid = callSid
                });

            await agentws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Ended call {callSid}");
            SetAgentOffCall();
        }

        private void SetAgentOffCall()
        {
            comboBox1.Enabled = true;
            comboBox1.SelectedItem = AgentStatus.Available;
        }

        private void AddIncomingCallUI(string callSid, string fromNumber)
        {
            if (incomingCallRows.ContainsKey(callSid)) return;
            var ctrl = new IncomingCallControl(callSid, fromNumber)
            {
                Dock = DockStyle.Top
            };



            ctrl.Accepted += async (_, __) =>
            {
                await AcceptCall(callSid, fromNumber);
                PanelIncomingCalls.Controls.Remove(ctrl);
                incomingCallRows.Remove(callSid);
            };

            PanelIncomingCalls.Controls.Add(ctrl);
            PanelIncomingCalls.Controls.SetChildIndex(ctrl, 0);

            incomingCallRows[callSid] = ctrl;
        }

        private void MicCapture()
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(8000, 16, 1)
            };

            waveIn.DataAvailable += async (s, e) =>
            {
                if (micMuted || string.IsNullOrEmpty(currentCallSid)) return;
                var pcm = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, pcm, e.BytesRecorded);
                var muLaw = new byte[pcm.Length / 2];
                for (int i = 0; i < muLaw.Length; i++)
                {
                    short sample = BitConverter.ToInt16(pcm, i * 2);
                    muLaw[i] = MuLawEncoder.LinearToMuLawSample(sample);
                }

                var payload = Convert.ToBase64String(muLaw);

                var msg = JsonSerializer.Serialize(
                    new
                    {
                        payload,
                        CallSid = currentCallSid
                    }
                 );
                await agentws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);

            };
            waveIn.StartRecording();
        }

        private void ToggleMicMute(bool mute)
        {
            micMuted = mute;
            if (waveIn == null) return;
        }

        /// <summary>
        /// When form is closed, close everything
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            waveIn?.StopRecording();
            waveOut?.Stop();
            agentws?.Dispose();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// When SendToDTMF button is clicked, send message to server to redirect that call to the DTMF line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Btn_SendToDTMF_Click(object sender, EventArgs e)
        {
            if (agentws == null || agentws.State != WebSocketState.Open)
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

            await agentws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Sent redirectDTMF for CallSid {currentCallSid}");
        }

        private void lb_Status_Click(object sender, EventArgs e)
        {

        }

        private void PanelIncomingCalls_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Status = (AgentStatus)comboBox1.SelectedIndex;
        }
    }
}