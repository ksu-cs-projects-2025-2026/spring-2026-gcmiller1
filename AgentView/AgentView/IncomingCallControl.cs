using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhoneNumbers;

namespace AgentView
{
    public partial class IncomingCallControl : UserControl
    {
        public string CallSid { get; }
        public string FromNumber { get; }
        public event EventHandler Accepted;
        public IncomingCallControl(string callSid, string fromNumber)
        {
            InitializeComponent();
            CallSid = callSid;
            FromNumber = fromNumber;
            this.Height = 40;
            label1.Text = $"Incoming Call: {FormatPhone(FromNumber)}";
        }

        public static string FormatPhone(string pn)
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            var number = phoneUtil.Parse(pn, null);
            return phoneUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Accepted?.Invoke(this, EventArgs.Empty);
        }

    }
}
