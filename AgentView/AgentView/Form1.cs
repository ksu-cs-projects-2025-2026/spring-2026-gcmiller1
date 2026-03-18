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
        private bool hold = false;
        private Dictionary<string, IncomingCallControl> incomingCallRows = new();
        private List<(string CallSid, string From)> pendingCalls = new();
        private AgentStatus Status;
        private OnCallControl activeCallControl;
        private bool micMuted = false;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            PopulateStatusDropdown();
            Status = AgentStatus.Available;
            comboBox1.SelectedItem = AgentStatus.Available;
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
            Status = AgentStatus.OnCall;
            var list = ((List<AgentStatus>)comboBox1.DataSource).ToList();
            if (!list.Contains(AgentStatus.OnCall)) list.Add(AgentStatus.OnCall);

            comboBox1.DataSource = null;
            comboBox1.DataSource = list;
            comboBox1.SelectedItem = AgentStatus.OnCall;
            comboBox1.Enabled = false;
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
                                    if (Status == AgentStatus.Available)
                                    {
                                        AddIncomingCallUI(callSid, from);
                                    }
                                    else
                                    {
                                        pendingCalls.Add((callSid, from));
                                    }
                                });
                            }
                            if (evtProp.GetString() == "endCall")
                            {
                                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                                Console.WriteLine($"Call ended: {callSid}");
                                this.Invoke(() =>
                                {
                                    if (activeCallControl != null && activeCallControl.CallSid == callSid && hold == false)
                                    {
                                        PanelActiveCall.Controls.Clear();
                                        PanelActiveCall.Visible = false;
                                        activeCallControl = null;
                                        currentCallSid = "";
                                        SetAgentOffCall();
                                        return;
                                    }

                                });
                            }

                            if (evtProp.GetString() == "callAnswered")
                            {
                                var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                                RemoveCallUI(callSid);
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

        private void FlushPendingCalls()
        {
            foreach (var call in pendingCalls)
            {
                AddIncomingCallUI(call.CallSid, call.From);
            }

            pendingCalls.Clear();
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
            PanelIncomingCalls.Visible = false;
            PanelActiveCall.Visible = true;
            PanelActiveCall.BringToFront();
            var callCtrl = new OnCallControl(callSid, fromNumber)
            {
                Dock = DockStyle.Fill,
                Size = new Size(558, 456)
            };
            PanelActiveCall.Controls.Add(callCtrl);
            activeCallControl = callCtrl;


            callCtrl.MuteUnmute += ToggleMicMute;
            callCtrl.SendToDTMF += async (_, __) => await SendToDTMF();

            callCtrl.CallEnded += async (_, __) =>
            {
                await EndCall(callSid);
                PanelActiveCall.Controls.Clear();
                PanelActiveCall.Visible = false;
                if (Status == AgentStatus.Available)
                {
                    PanelIncomingCalls.Visible = true;
                }
            };
            callCtrl.OnHold += async (isOnHold) =>
            {
                if (isOnHold)
                    await PutOnHold(callSid);
                else
                {
                    await TakeOffHold(callSid);
                    currentCallSid = callSid;
                }

            };
        }

        private async Task PutOnHold(string callSid)
        {
            if (agentws == null || agentws.State != WebSocketState.Open) return;
            hold = true;
            var msg = JsonSerializer.Serialize(new
            {
                Action = "putOnHold",
                CallSid = callSid
            });
            await agentws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task TakeOffHold(string callSid)
        {
            if (agentws == null || agentws.State != WebSocketState.Open) return;

            var msg = JsonSerializer.Serialize(new
            {
                Action = "takeOffHold",
                CallSid = callSid
            });
            hold = false;
            await agentws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task EndCall(string callSid)
        {
            currentCallSid = callSid;
            SetAgentOffCall();
            var msg = JsonSerializer.Serialize(
                new
                {
                    Action = "endCall",
                    CallSid = callSid
                });

            await agentws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Ended call {callSid}");
            //RemoveCallUI(callSid);
            activeCallControl = null;
        }

        private void SetAgentOffCall()
        {
            comboBox1.Enabled = true;

            var list = ((List<AgentStatus>)comboBox1.DataSource).ToList();
            if (!list.Contains(AgentStatus.Available)) list.Add(AgentStatus.Available);

            comboBox1.DataSource = null;
            comboBox1.DataSource = list;
            comboBox1.SelectedItem = AgentStatus.Available;

            Status = AgentStatus.Available;
        }

        private void AddIncomingCallUI(string callSid, string fromNumber)
        {
            if (incomingCallRows.ContainsKey(callSid)) return;
            var ctrl = new IncomingCallControl(callSid, fromNumber);
            ctrl.Width = PanelIncomingCalls.ClientSize.Width - 20;


            ctrl.Accepted += async (_, __) =>
            {
                await AcceptCall(callSid, fromNumber);
                RemoveCallUI(callSid);
            };

            PanelIncomingCalls.Controls.Add(ctrl);
            PanelIncomingCalls.Controls.SetChildIndex(ctrl, 0);

            incomingCallRows[callSid] = ctrl;
        }

        private void RemoveCallUI(string callSid)
        {
            if (incomingCallRows.TryGetValue(callSid, out var incomingCtrl))
            {
                PanelIncomingCalls.Controls.Remove(incomingCtrl);
                incomingCtrl.Dispose();
                incomingCallRows.Remove(callSid);
            }

            pendingCalls.RemoveAll(c => c.CallSid == callSid);
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

        private async Task SendToDTMF()
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


        private void lb_Status_Click(object sender, EventArgs e)
        {

        }

        private void PanelIncomingCalls_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null) return;
            Status = (AgentStatus)comboBox1.SelectedItem;
            if (Status == AgentStatus.OnCall) return;
            if (activeCallControl != null)
            {
                PanelActiveCall.Visible = true;
                PanelIncomingCalls.Visible = false;
                return;
            }
            if (Status == AgentStatus.Available)
            {
                PanelIncomingCalls.Visible = true;
                PanelActiveCall.Visible = false;

                foreach (var kvp in incomingCallRows)
                {
                    var ctrl = kvp.Value;
                    if (!PanelIncomingCalls.Controls.Contains(ctrl))
                    {
                        PanelIncomingCalls.Controls.Add(ctrl);
                        PanelIncomingCalls.Controls.SetChildIndex(ctrl, 0);
                    }
                }

                FlushPendingCalls();
            }
            else
            {
                PanelIncomingCalls.Visible = false;
            }
        }

        private void btn_History_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}