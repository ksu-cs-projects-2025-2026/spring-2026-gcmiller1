using NAudio.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentView
{
    public class AgentController
    {
        private readonly MainView view;
        private readonly CommService commService;
        private readonly AudioService audioService;
        private readonly CallManager callManager;
        private readonly Agent agent;

        private string currentCallSid;
        private bool hold;

        private const string AgentWsUrl = "wss://uncoquettishly-bilgiest-bronson.ngrok-free.dev/agent";

        public AgentController(MainView view)
        {
            this.view = view;
            commService = new CommService();
            audioService = new AudioService();
            callManager = new CallManager();
            agent = new Agent();

            WireViewEvents();
        }

        public async Task StartAsync()
        {
            await ConnectToIVRServerAsync();
        }

        /// <summary>
        /// Connects to the server
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToIVRServerAsync()
        {
            try
            {
                audioService.StartPlayback();

                audioService.StartMicCapture(async muLaw =>
                {
                    if (string.IsNullOrEmpty(currentCallSid))
                    {
                        return;
                    }

                    var payload = Convert.ToBase64String(muLaw);

                    await commService.SendJsonAsync(new
                    {
                        payload,
                        CallSid = currentCallSid
                    });
                });

                await commService.ConnectAsync(AgentWsUrl);

                _ = Task.Run(ReceiveCallerAudio);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection failed: {ex}");
            }
        }

        /// <summary>
        /// Wires event handlers in the view to their corresponding methods
        /// </summary>
        private void WireViewEvents()
        {
            view.AcceptCallRequested += AcceptCall;
            view.EndCallRequested += EndCall;
            view.HoldRequested += PutOnHold;
            view.ResumeRequested += TakeOffHold;
            view.MuteToggled += ToggleMicMute;
            view.DtmfRequested += SendToDTMF;
            view.CardVerificationRequested += StartCardVerification;

            view.StatusChanged += status =>
            {
                agent.SetStatus(status);
            };
        }

        /// <summary>
        /// Tells the server to start listening to keypad inputs during the call.
        /// </summary>
        /// <param name="callSid">The id of the call</param>
        /// <returns></returns>
        private async Task StartCardVerification(string callSid)
        {
            await commService.SendJsonAsync(new
            {
                Action = "startCardVerification",
                CallSid = callSid
            });
        }

        /// <summary>
        /// Listener for everything coming from the server. Handles event messages and processes incoming audio.
        /// </summary>
        /// <returns></returns>

        private async Task ReceiveCallerAudio()
        {
            await commService.ReceiveLoopAsync(
                async json =>
                {
                    using var doc = JsonDocument.Parse(json);

                    if (!doc.RootElement.TryGetProperty("event", out var evtProp))
                    {
                        return;
                    }

                    var evt = evtProp.GetString();

                    if (evt == "start")
                    {
                        currentCallSid = doc.RootElement.GetProperty("CallSid").GetString();
                        Console.WriteLine($"Call started: {currentCallSid}");
                    }

                    if (evt == "IncomingCall")
                    {
                        string callSid = doc.RootElement.GetProperty("CallSid").GetString();
                        string from = doc.RootElement.GetProperty("From").GetString();

                        var call = new Call
                        {
                            CallSid = callSid,
                            From = from
                        };
                        callManager.AddIncomingCall(call);

                        view.Invoke(() =>
                        {
                            if (agent.Status == AgentStatus.Available)
                            {
                                view.AddIncomingCallUI(callSid, from);
                            }
                            else
                            {
                                view.AddPendingCall(callSid, from);
                            }
                        });
                    }

                    if (evt == "endCall")
                    {
                        var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                        Console.WriteLine($"Call ended: {callSid}");

                        view.Invoke(() =>
                        {
                            if (view.HasActiveCallControl(callSid) && hold == false)
                            {
                                view.ClearActiveCall();
                                currentCallSid = "";
                                view.SetAgentOffCall();
                                agent.SetStatus(AgentStatus.Available);
                                callManager.EndCall(callSid);
                            }
                            else
                            {
                                view.RemoveCallUI(callSid);
                                callManager.EndCall(callSid);
                            }
                        });
                    }

                    if (evt == "cardVerificationResult")
                    {
                        var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                        var success = doc.RootElement.GetProperty("Success").GetBoolean();

                        view.Invoke(() =>
                        {
                            view.ShowVerificationResult(callSid, success);
                        });
                    }

                    if (evt == "callAnswered")
                    {
                        var callSid = doc.RootElement.GetProperty("CallSid").GetString();

                        view.Invoke(() =>
                        {
                            view.RemoveCallUI(callSid);
                            callManager.RemovePendingCall(callSid);
                        });
                    }

                    if (evt == "transcriptUpdate")
                    {
                        var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                        var text = doc.RootElement.GetProperty("Text").GetString();
                        var isFinal = doc.RootElement.GetProperty("IsFinal").GetBoolean();
                        Console.WriteLine($"transcriptUpdate received: callSid={callSid}, text='{text}', isFinal={isFinal}");
                        view.Invoke(() =>
                        {
                            view.ShowTranscript(callSid, text, isFinal);
                        });
                    }

                    if (evt == "sentimentUpdate")
                    {
                        var callSid = doc.RootElement.GetProperty("CallSid").GetString();
                        var score = doc.RootElement.GetProperty("Score").GetDouble();
                        var label = doc.RootElement.GetProperty("Label").GetString();

                        view.Invoke(() =>
                        {
                            view.ShowSentiment(callSid, score, label);
                        });
                    }

                    await Task.CompletedTask;
                },
                async binary =>
                {
                    audioService.PlayMuLawAudio(binary);
                    await Task.CompletedTask;
                });
        }

        /// <summary>
        /// Handles accepting a call.
        /// </summary>
        /// <param name="callSid">the ID of the call that is being answered</param>
        /// <param name="fromNumber">The phone number the call is coming from</param>
        /// <returns></returns>
        private async Task AcceptCall(string callSid, string fromNumber)
        {
            currentCallSid = callSid;

            await commService.SendJsonAsync(new
            {
                Action = "acceptCall",
                CallSid = callSid
            });

            Console.WriteLine($"Accepted call {callSid}");

            var pending = callManager.GetPendingCall(callSid);
            if (pending == null)
            {
                pending = new Call { CallSid = callSid, From = fromNumber };
            }

            callManager.AcceptCall(pending);
            agent.SetStatus(AgentStatus.OnCall);

            view.SetAgentOnCall();
            view.RemoveCallUI(callSid);
            view.ShowActiveCall(callSid, fromNumber);
        }

        /// <summary>
        /// Handles putting the caller on hold.
        /// </summary>
        /// <param name="callSid">The ID of the call to be put on hold</param>
        /// <returns></returns>
        private async Task PutOnHold(string callSid)
        {
            if (commService.State != System.Net.WebSockets.WebSocketState.Open)
            {
                return;
            }

            hold = true;

            await commService.SendJsonAsync(new
            {
                Action = "putOnHold",
                CallSid = callSid
            });

            if (callManager.ActiveCall != null)
            {
                callManager.ActiveCall.IsOnHold = true;
            }
        }

        /// <summary>
        /// Handles taking the caller off hold.
        /// </summary>
        /// <param name="callSid">The ID of the call to be taken off hold</param>
        /// <returns></returns>
        private async Task TakeOffHold(string callSid)
        {
            if (commService.State != System.Net.WebSockets.WebSocketState.Open)
            {
                return;
            }

            await commService.SendJsonAsync(new
            {
                Action = "takeOffHold",
                CallSid = callSid
            });

            hold = false;
            currentCallSid = callSid;

            if (callManager.ActiveCall != null)
            {
                callManager.ActiveCall.IsOnHold = false;
            }
        }

        /// <summary>
        /// Handles ending a phone call.
        /// </summary>
        /// <param name="callSid">The ID of the call to be ended</param>
        /// <returns></returns>
        private async Task EndCall(string callSid)
        {
            currentCallSid = callSid;
            view.SetAgentOffCall();
            agent.SetStatus(AgentStatus.Available);

            await commService.SendJsonAsync(new
            {
                Action = "endCall",
                CallSid = callSid
            });

            Console.WriteLine($"Ended call {callSid}");
            callManager.EndCall(callSid);

            view.ClearActiveCall();
            currentCallSid = "";
        }

        /// <summary>
        /// Toggles muting the agent's microphone in a phone call.
        /// </summary>
        /// <param name="mute">whether they are muting or unmuting the call</param>
        /// <returns></returns>
        private Task ToggleMicMute(bool mute)
        {
            audioService.SetMute(mute);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles sending the active phone call to the DTMF system where the caller dials their sensitive number data.
        /// </summary>
        /// <returns></returns>
        private async Task SendToDTMF()
        {
            await commService.SendJsonAsync(new
            {
                Action = "redirectDTMF",
                CallSid = currentCallSid
            });

            Console.WriteLine($"Sent redirectDTMF for CallSid {currentCallSid}");
        }
    }
}
