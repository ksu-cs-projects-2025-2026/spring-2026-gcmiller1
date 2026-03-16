using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace AgentView
{
    public partial class OnCallControl : UserControl
    {
        public string CallSid { get; }
        public string FromNumber { get; }
        private Stopwatch stopwatch = new Stopwatch();
        public event EventHandler CallEnded;
        public event Action<bool> MuteUnmute;
        public event EventHandler SendToDTMF;
        private bool IsMuted;
        public OnCallControl(string callSid, string from)
        {
            CallSid = callSid;
            FromNumber = from;
            IsMuted = false;
            InitializeComponent();
            timer_Call.Interval = 1000;
            timer_Call.Tick += Timer_Call_Tick;
            label_FromNumber.Text = FromNumber;
            stopwatch.Start();
            timer_Call.Start();
        }

        private void Timer_Call_Tick(object sender, EventArgs e)
        {
            label_Timer.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
        }

        private void btn_EndCall_Click(object sender, EventArgs e)
        {
            timer_Call.Stop();
            stopwatch.Stop();
            CallEnded?.Invoke(this, EventArgs.Empty);
        }

        private void btn_MuteMic_Click(object sender, EventArgs e)
        {
            if (IsMuted == false)
            {
                btn_MuteMic.BackColor = SystemColors.ButtonHighlight;
                btn_MuteMic.BackgroundImage = Properties.Resources.microphone_mute;
                btn_MuteMic.BackgroundImageLayout = ImageLayout.Stretch;
                MuteUnmute?.Invoke(true);
                IsMuted = true;
            }
            else
            {
                btn_MuteMic.BackColor = SystemColors.ButtonHighlight;
                btn_MuteMic.BackgroundImage = Properties.Resources.microphone_105;
                btn_MuteMic.BackgroundImageLayout = ImageLayout.Stretch;
                MuteUnmute?.Invoke(false);
                IsMuted = false;
            }

        }

        private void btn_DTMF_Click(object sender, EventArgs e)
        {
            SendToDTMF?.Invoke(this, EventArgs.Empty);
        }
    }
}
