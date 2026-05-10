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

    public partial class MainView : Form
    {
        private enum MainContentView
        {
            Home,
            History,
            Contacts
        }

        private MainContentView currentView = MainContentView.Home;
        public event Func<string, string, Task> AcceptCallRequested;
        public event Func<string, Task> EndCallRequested;
        public event Func<string, Task> HoldRequested;
        public event Func<string, Task> ResumeRequested;
        public event Func<bool, Task> MuteToggled;
        public event Func<Task> DtmfRequested;
        public event Func<string, Task> CardVerificationRequested;
        public event Func<Contact, Task> ContactCallRequested;
        public event Action<AgentStatus> StatusChanged;

        private List<Contact> loadedContacts = new();
        private readonly Dictionary<string, IncomingCallControl> incomingCallRows = new();
        private readonly List<(string CallSid, string From)> pendingCalls = new();
        private OnCallForm activeCallForm;
        private OnCallControl activeCallControl;



        public MainView()
        {
            InitializeComponent();
            Load += MainView_Load;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;

            PopulateStatusDropdown();
            comboBox1.SelectedItem = AgentStatus.Available;
        }

        /// <summary>
        /// Populates the dropdown menu for agent status
        /// </summary>
        private void PopulateStatusDropdown()
        {
            var statuses = Enum.GetValues(typeof(AgentStatus))
                .Cast<AgentStatus>()
                .Where(s => s != AgentStatus.OnCall)
                .ToList();

            comboBox1.DataSource = statuses;
        }

        /// <summary>
        /// Loads the main view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainView_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedItem = AgentStatus.Available;
            SetSelectedMenuButton(MainContentView.Home);
            btn_AddNewContact.Visible = false;
        }

        /// <summary>
        /// Updates the view for when an agent begins a call
        /// </summary>
        public void SetAgentOnCall()
        {
            var list = ((List<AgentStatus>)comboBox1.DataSource).ToList();
            if (!list.Contains(AgentStatus.OnCall))
            {
                list.Add(AgentStatus.OnCall);
            }

            comboBox1.DataSource = null;
            comboBox1.DataSource = list;
            comboBox1.SelectedItem = AgentStatus.OnCall;
            comboBox1.Enabled = false;
        }

        /// <summary>
        /// Shows in the OnCallControl if the caller successfully verified their card
        /// </summary>
        /// <param name="callSid">the id of the call</param>
        /// <param name="success">if the verification was successful</param>
        public void ShowVerificationResult(string callSid, bool success)
        {
            if (activeCallControl != null && activeCallControl.CallSid == callSid)
            {
                activeCallControl.ShowVerificationResult(success);
            }
        }

        public void ShowVerificationStarted(string callSid)
        {
            if (activeCallControl != null && activeCallControl.CallSid == callSid)
            {
                activeCallControl.BeginCardVerification();
            }
        }

        /// <summary>
        /// Updates the view for when a call ends
        /// </summary>
        public void SetAgentOffCall()
        {
            comboBox1.Enabled = true;

            var list = ((List<AgentStatus>)comboBox1.DataSource).ToList();
            if (!list.Contains(AgentStatus.Available))
            {
                list.Add(AgentStatus.Available);
            }

            comboBox1.DataSource = null;
            comboBox1.DataSource = list;
            comboBox1.SelectedItem = AgentStatus.Available;
        }

        /// <summary>
        /// Adds new IncomingCallControl when a new phone call is incoming
        /// </summary>
        /// <param name="callSid">the ID of the incoming call</param>
        /// <param name="fromNumber">the phone number of the caller</param>
        public void AddIncomingCallUI(string callSid, string fromNumber)
        {
            if (incomingCallRows.ContainsKey(callSid))
            {
                return;
            }

            var ctrl = new IncomingCallControl(callSid, fromNumber);
            ctrl.Width = PanelIncomingCalls.ClientSize.Width - 20;

            ctrl.Accepted += async (_, __) =>
            {
                if (AcceptCallRequested != null)
                {
                    await AcceptCallRequested(callSid, fromNumber);
                }
            };

            incomingCallRows[callSid] = ctrl;

            if (currentView == MainContentView.Home)
            {
                PanelIncomingCalls.Controls.Add(ctrl);
                PanelIncomingCalls.Controls.SetChildIndex(ctrl, 0);
            }
        }

        /// <summary>
        /// Removes the IncomingCallControl from the UI
        /// </summary>
        /// <param name="callSid"></param>
        public void RemoveCallUI(string callSid)
        {
            if (incomingCallRows.TryGetValue(callSid, out var incomingCtrl))
            {
                PanelIncomingCalls.Controls.Remove(incomingCtrl);
                incomingCtrl.Dispose();
                incomingCallRows.Remove(callSid);
            }

            pendingCalls.RemoveAll(c => c.CallSid == callSid);
        }

        /// <summary>
        /// Adds an incoming call to a list of pending calls if the call is not to be drawn yet in the view
        /// </summary>
        /// <param name="callSid">the ID of the pending call</param>
        /// <param name="from">the phone number of the pending call</param>
        public void AddPendingCall(string callSid, string from)
        {
            if (!pendingCalls.Any(c => c.CallSid == callSid))
            {
                pendingCalls.Add((callSid, from));
            }
        }

        /// <summary>
        /// Flushes the pending calls and draws them to the view when it is an appropriate time
        /// </summary>
        public void FlushPendingCalls()
        {
            foreach (var call in pendingCalls.ToList())
            {
                AddIncomingCallUI(call.CallSid, call.From);
            }

            pendingCalls.Clear();
        }

        /// <summary>
        /// Shows the active call and call controls
        /// </summary>
        /// <param name="callSid">the ID of the call to be activated</param>
        /// <param name="fromNumber">the phone number of that caller</param>
        public void ShowActiveCall(string callSid, string fromNumber)
        {
            PanelIncomingCalls.Visible = false;
            PanelActiveCall.Visible = false;

            if (activeCallForm != null && !activeCallForm.IsDisposed)
            {
                activeCallForm.Close();
                activeCallForm.Dispose();
                activeCallForm = null;
                activeCallControl = null;
            }

            var callForm = new OnCallForm(callSid, fromNumber);

            callForm.StartPosition = FormStartPosition.Manual;
            callForm.Left = this.Right + 10;
            callForm.Top = this.Top;

            activeCallForm = callForm;
            activeCallControl = callForm.CallControl;

            activeCallControl.MuteUnmute += async mute =>
            {
                if (MuteToggled != null)
                {
                    await MuteToggled(mute);
                }
            };

            activeCallControl.SendToDTMF += async (_, __) =>
            {
                if (DtmfRequested != null)
                {
                    await DtmfRequested();
                }
            };

            activeCallControl.CallEnded += async (_, __) =>
            {
                if (EndCallRequested != null)
                {
                    await EndCallRequested(callSid);
                }
            };

            activeCallControl.OnHold += async isOnHold =>
            {
                if (isOnHold)
                {
                    if (HoldRequested != null)
                    {
                        await HoldRequested(callSid);
                    }
                }
                else
                {
                    if (ResumeRequested != null)
                    {
                        await ResumeRequested(callSid);
                    }
                }
            };

            activeCallControl.VerifyCardRequested += async (_, __) =>
            {
                if (CardVerificationRequested != null)
                {
                    await CardVerificationRequested(callSid);
                }
            };

            callForm.FormClosed += (_, __) =>
            {
                activeCallControl = null;
                activeCallForm = null;
            };

            callForm.Show(this);
            callForm.BringToFront();
            callForm.Activate();
        }

        /// <summary>
        /// Checks if the active call is a given callSid
        /// </summary>
        /// <param name="callSid">the ID of the call to be checked</param>
        /// <returns></returns>
        public bool HasActiveCallControl(string callSid)
        {
            return activeCallControl != null && activeCallControl.CallSid == callSid;
        }

        /// <summary>
        /// Clears the active call for the agent
        /// </summary>
        public void ClearActiveCall()
        {
            var form = activeCallForm;

            activeCallForm = null;
            activeCallControl = null;
            PanelActiveCall.Visible = false;

            if (form != null && !form.IsDisposed)
            {
                form.Close();
            }
        }

        /// <summary>
        /// The functionality of the CallForm when an outbound call is being made
        /// </summary>
        /// <param name="callSid"></param>
        /// <param name="phoneNumber"></param>
        public void ShowOutboundCalling(string callSid, string phoneNumber)
        {
            PanelIncomingCalls.Visible = false;
            PanelActiveCall.Visible = false;

            if (activeCallForm != null && !activeCallForm.IsDisposed)
            {
                activeCallForm.Close();
                activeCallForm.Dispose();
                activeCallForm = null;
                activeCallControl = null;
            }

            var callForm = new OnCallForm(callSid, phoneNumber, startConnected: false);

            callForm.StartPosition = FormStartPosition.Manual;
            callForm.Left = this.Right + 10;
            callForm.Top = this.Top;

            activeCallForm = callForm;
            activeCallControl = callForm.CallControl;

            activeCallControl.MuteUnmute += async mute =>
            {
                if (MuteToggled != null)
                {
                    await MuteToggled(mute);
                }
            };

            activeCallControl.SendToDTMF += async (_, __) =>
            {
                if (DtmfRequested != null)
                {
                    await DtmfRequested();
                }
            };

            activeCallControl.CallEnded += async (_, __) =>
            {
                if (EndCallRequested != null)
                {
                    await EndCallRequested(callSid);
                }
            };

            activeCallControl.OnHold += async isOnHold =>
            {
                if (isOnHold)
                {
                    if (HoldRequested != null)
                    {
                        await HoldRequested(callSid);
                    }
                }
                else
                {
                    if (ResumeRequested != null)
                    {
                        await ResumeRequested(callSid);
                    }
                }
            };

            activeCallControl.VerifyCardRequested += async (_, __) =>
            {
                if (CardVerificationRequested != null)
                {
                    await CardVerificationRequested(callSid);
                }
            };

            callForm.FormClosed += (_, __) =>
            {
                activeCallControl = null;
                activeCallForm = null;
            };

            callForm.Show(this);
            callForm.BringToFront();
            callForm.Activate();
        }

        /// <summary>
        /// Lets the CallControl know that their outbound phone call has been answered
        /// </summary>
        /// <param name="callSid"></param>
        public void MarkOutboundCallAnswered(string callSid)
        {
            if (activeCallControl != null && activeCallControl.CallSid == callSid)
            {
                activeCallControl.StartConnectedCall();
            }
        }

        /// <summary>
        /// Shows the transcript of the active call in the CallForm
        /// </summary>
        /// <param name="callSid"></param>
        /// <param name="text"></param>
        /// <param name="isFinal"></param>
        public void ShowTranscript(string callSid, string text, bool isFinal)
        {
            if (activeCallForm != null && activeCallForm.CallControl.CallSid == callSid)
            {
                activeCallForm.ShowTranscript(text, isFinal);
            }
        }

        /// <summary>
        /// Shows the sentiment score of the active all in the CallForm
        /// </summary>
        /// <param name="callSid"></param>
        /// <param name="score"></param>
        /// <param name="label"></param>
        public void ShowSentiment(string callSid, double score, string label)
        {
            if (activeCallForm != null && activeCallForm.CallControl.CallSid == callSid)
            {
                activeCallForm.ShowSentiment(score, label);
            }
        }

        /// <summary>
        /// When form is closed, close everything
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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
            if (comboBox1.SelectedItem == null)
            {
                return;
            }

            var status = (AgentStatus)comboBox1.SelectedItem;
            if (status == AgentStatus.OnCall)
            {
                return;
            }

            if (activeCallControl != null)
            {
                if (currentView == MainContentView.Home)
                {
                    PanelIncomingCalls.Visible = false;
                }

                if (activeCallForm != null && !activeCallForm.IsDisposed)
                {
                    activeCallForm.BringToFront();
                }

                return;
            }

            if (status == AgentStatus.Available)
            {
                if (currentView == MainContentView.Home)
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
            }
            else
            {
                if (currentView == MainContentView.Home)
                {
                    PanelIncomingCalls.Visible = false;
                }
            }

            StatusChanged?.Invoke(status);
        }

        /// <summary>
        /// Populates the content panel with the Home page
        /// </summary>
        private void ShowHomeView()
        {
            currentView = MainContentView.Home;
            btn_AddNewContact.Visible = false;
            SetSelectedMenuButton(MainContentView.Home);
            PanelIncomingCalls.Controls.Clear();
            PanelIncomingCalls.Visible = true;
            PanelActiveCall.Visible = false;

            foreach (var kvp in incomingCallRows)
            {
                var ctrl = kvp.Value;

                if (!ctrl.IsDisposed)
                {
                    PanelIncomingCalls.Controls.Add(ctrl);
                    PanelIncomingCalls.Controls.SetChildIndex(ctrl, 0);
                }
            }

            FlushPendingCalls();
        }

        /// <summary>
        /// Populates the content panel with the History page
        /// </summary>
        private void ShowHistoryView()
        {
            currentView = MainContentView.History;
            btn_AddNewContact.Visible = false;
            SetSelectedMenuButton(MainContentView.History);
            PanelIncomingCalls.Controls.Clear();
            PanelIncomingCalls.Visible = true;
            PanelActiveCall.Visible = false;

            var filePath = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName,
                "CallSummary.json"
            );

            if (!File.Exists(filePath))
            {
                PanelIncomingCalls.Controls.Add(new Label
                {
                    Text = "No call history found.",
                    Dock = DockStyle.Top,
                    Height = 40
                });

                return;
            }

            var json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                PanelIncomingCalls.Controls.Add(new Label
                {
                    Text = "No call history found.",
                    Dock = DockStyle.Top,
                    Height = 40
                });

                return;
            }

            List<CallSummary> summaries;

            if (json.TrimStart().StartsWith("["))
            {
                summaries = JsonSerializer.Deserialize<List<CallSummary>>(json) ?? new List<CallSummary>();
            }
            else
            {
                var singleSummary = JsonSerializer.Deserialize<CallSummary>(json);
                summaries = singleSummary != null ? new List<CallSummary> { singleSummary } : new List<CallSummary>();
            }
            var label = new Label();
            foreach (var summary in summaries.AsEnumerable().Reverse())
            {
                var direction = summary.Inbound ? "Inbound" : "Outbound";
                var numberLine = $"{summary.FromNumber} ({direction})"; // pretty sick
                if (summary.Answered == true)
                {
                    label = new Label
                    {
                        AutoSize = false,
                        Height = 110,
                        Dock = DockStyle.Top,
                        Padding = new Padding(10),
                        BorderStyle = BorderStyle.FixedSingle,
                        Text =
                            $"From: {numberLine}{Environment.NewLine}" +
                            $"Start: {summary.CallStartTime}{Environment.NewLine}" +
                            $"Length: {summary.CallLength}{Environment.NewLine}" +
                            $"Sentiment: {summary.CallSentiment:F2}{Environment.NewLine}" +
                            $"Card Verified: {(summary.CardVerified ? "Yes" : "No")}"
                    };

                }

                else
                {
                    label = new Label
                    {
                        AutoSize = false,
                        Height = 110,
                        Dock = DockStyle.Top,
                        Padding = new Padding(10),
                        BorderStyle = BorderStyle.FixedSingle,
                        Text =
                            $"Call Missed{Environment.NewLine}" +
                            $"{numberLine}{Environment.NewLine}" +
                            $"Time: {summary.CallEndTime}{Environment.NewLine}"
                    };
                }


                PanelIncomingCalls.Controls.Add(label);
                PanelIncomingCalls.Controls.SetChildIndex(label, 0);
            }
        }

        private void AddContactControlToPanel(Contact contact)
        {
            var ctrl = new ContactControl(contact)
            {
                Width = PanelIncomingCalls.ClientSize.Width - 20,
                Dock = DockStyle.Top
            };

            ctrl.ContactUpdated += (_, __) =>
            {
                SaveContactsToJson();
            };

            ctrl.ContactDeleted += (_, __) =>
            {
                DeleteContact(ctrl);
            };

            ctrl.CallRequested += async (_, contactToCall) =>
            {
                if (ContactCallRequested != null)
                {
                    await ContactCallRequested(contactToCall);
                }
            };

            PanelIncomingCalls.Controls.Add(ctrl);
            PanelIncomingCalls.Controls.SetChildIndex(ctrl, 0);
        }

        /// <summary>
        /// Populates the content panel with the Contacts page
        /// </summary>
        private void ShowContactsView()
        {
            currentView = MainContentView.Contacts;
            btn_AddNewContact.Visible = true;
            SetSelectedMenuButton(MainContentView.Contacts);
            PanelIncomingCalls.Controls.Clear();
            PanelIncomingCalls.Visible = true;
            PanelActiveCall.Visible = false;

            var filePath = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName,
                "Contacts.json"
            );

            if (!File.Exists(filePath))
            {
                PanelIncomingCalls.Controls.Add(new Label
                {
                    Text = "No contacts found.",
                    Dock = DockStyle.Top,
                    Height = 40,
                    Padding = new Padding(10)
                });

                return;
            }

            var json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                PanelIncomingCalls.Controls.Add(new Label
                {
                    Text = "No contacts found.",
                    Dock = DockStyle.Top,
                    Height = 40,
                    Padding = new Padding(10)
                });

                return;
            }

            List<Contact> contacts;

            if (json.TrimStart().StartsWith("["))
            {
                contacts = JsonSerializer.Deserialize<List<Contact>>(json) ?? new List<Contact>();
            }
            else
            {
                var singleContact = JsonSerializer.Deserialize<Contact>(json);
                contacts = singleContact != null
                    ? new List<Contact> { singleContact }
                    : new List<Contact>();
            }

            foreach (var contact in contacts
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Reverse())
            {
                AddContactControlToPanel(contact);
            }
            loadedContacts = contacts;
        }

        private void DeleteContact(ContactControl ctrl)
        {
            loadedContacts.Remove(ctrl.Contact);
            PanelIncomingCalls.Controls.Remove(ctrl);
            ctrl.Dispose();
            SaveContactsToJson();
        }

        private void SaveContactsToJson()
        {
            var filePath = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName,
                "Contacts.json"
            );

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(filePath, JsonSerializer.Serialize(loadedContacts, options));
        }

        private void btn_History_Click(object sender, EventArgs e)
        {
            ShowHistoryView();
        }

        private void btn_Home_Click(object sender, EventArgs e)
        {
            ShowHomeView();
        }

        private void btn_Contacts_Click(object sender, EventArgs e)
        {
            ShowContactsView();
        }

        private void SetSelectedMenuButton(MainContentView selectedView)
        {
            btn_Home.BackColor = SystemColors.ControlLightLight;
            btn_History.BackColor = SystemColors.ControlLightLight;
            btn_Contacts.BackColor = SystemColors.ControlLightLight;

            if (selectedView == MainContentView.Home)
            {
                btn_Home.BackColor = SystemColors.ControlLight;
            }
            else if (selectedView == MainContentView.History)
            {
                btn_History.BackColor = SystemColors.ControlLight;
            }
            else if (selectedView == MainContentView.Contacts)
            {
                btn_Contacts.BackColor = SystemColors.ControlLight;
            }
        }

        private void btn_AddNewContact_Click(object sender, EventArgs e)
        {
            var contact = new Contact
            {
                FirstName = "",
                LastName = "",
                PhoneNumber = "",
                Email = "",
                CreatedOn = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")
            };

            using var form = new ContactForm(contact);

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (loadedContacts.Any(c => c.PhoneNumber == form.Contact.PhoneNumber))
                {
                    MessageBox.Show(
                        "A contact with this phone number already exists.",
                        "Duplicate Contact",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }
                loadedContacts.Add(form.Contact);
                SaveContactsToJson();

                AddContactControlToPanel(form.Contact);
            }
        }
    }
}