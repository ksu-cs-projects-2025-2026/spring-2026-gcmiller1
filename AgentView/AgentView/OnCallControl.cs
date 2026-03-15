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
        public OnCallControl(string callSid, string from)
        {
            CallSid = callSid;
            FromNumber = from;
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
    }
}
