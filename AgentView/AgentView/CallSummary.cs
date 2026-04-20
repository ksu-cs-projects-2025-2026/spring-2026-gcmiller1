using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentView
{
    public class CallSummary
    {
        public Contact CallContact { get; set; }

        public string CallStartTime { get; set; }

        public string CallEndTime { get; set; }

        public string Notes { get; set; }

        public string CallLength { get; set; }


    }
}
