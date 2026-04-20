using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgentView
{
    public partial class OnCallForm : Form
    {
        public OnCallControl CallControl { get; }

        private TextBox txtTranscript;
        private Label lb_Sentiment;
        public OnCallForm(string callSid, string fromNumber)
        {
            Text = $"Active Call - {fromNumber}";
            TopMost = true;
            Width = 350;
            Height = 700;

            txtTranscript = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Bottom,
                Height = 200
            };

            lb_Sentiment = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "Sentiment: Neutral (0.00)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            CallControl = new OnCallControl(callSid, fromNumber)
            {
                Dock = DockStyle.Fill
            };

            Controls.Add(CallControl);
            Controls.Add(txtTranscript);
            Controls.Add(lb_Sentiment);
        }

        public void ShowSentiment(double score, string label)
        {
            lb_Sentiment.Text = $"Sentiment: {label} ({score:F2})";

            if (label == "Negative")
            {
                lb_Sentiment.ForeColor = Color.Red;
            }
            else if (label == "Positive")
            {
                lb_Sentiment.ForeColor = Color.Green;
            }
            else
            {
                lb_Sentiment.ForeColor = Color.Black;
            }
        }

        public void ShowTranscript(string text, bool isFinal)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            if (isFinal)
            {
                txtTranscript.AppendText(text + Environment.NewLine);
            }
        }

        private void tb_LiveTranscript_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

