using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentView
{
    public class Agent
    {
        /// <summary>
        /// The status, defaults to Avaiable when launched.
        /// </summary>
        public AgentStatus Status { get; private set; } = AgentStatus.Available;

        /// <summary>
        /// Sets the status of the agent
        /// </summary>
        /// <param name="status">The status of the agent to be set</param>
        public void SetStatus(AgentStatus status)
        {
            Status = status;
        }
    }
}
