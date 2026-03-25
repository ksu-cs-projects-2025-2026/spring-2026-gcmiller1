using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentView
{
    public class Call
    {
        /// <summary>
        /// The ID of the call
        /// </summary>
        public string CallSid { get; set; }
        /// <summary>
        /// The phone number of the caller
        /// </summary>
        public string From { get; set; }
        /// <summary>
        /// If the call is on hold or not
        /// </summary>
        public bool IsOnHold { get; set; }
    }
}
