using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentView
{
    /// <summary>
    /// The possible statuses the agent can have
    /// </summary>
    public enum AgentStatus
    {
        Available,
        Unavailable,
        OnCall,
        Busy,
        Break,
        Training,
        Meeting,
        Idle,
        Offline
    }
}
