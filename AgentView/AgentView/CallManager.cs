using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentView
{
    public class CallManager
    {
        /// <summary>
        /// List of calls waiting to be answered
        /// </summary>
        public List<Call> PendingCalls { get; } = new();

        /// <summary>
        /// The phone call the agent is currently on
        /// </summary>
        public Call ActiveCall { get; private set; }

        /// <summary>
        /// Handles when a new phone call is incoming
        /// </summary>
        /// <param name="call"></param>
        public void AddIncomingCall(Call call)
        {
            if (PendingCalls.Any(c => c.CallSid == call.CallSid))
            {
                return;
            }

            PendingCalls.Add(call);
        }

        /// <summary>
        /// Gets the Call object that matches a callSid
        /// </summary>
        /// <param name="callSid">the ID of the call to be gotten</param>
        /// <returns></returns>
        public Call GetPendingCall(string callSid)
        {
            return PendingCalls.FirstOrDefault(c => c.CallSid == callSid);
        }

        /// <summary>
        /// Handles removing a pending call from the list of pending calls
        /// </summary>
        /// <param name="callSid">The ID of the call to be removed</param>
        public void RemovePendingCall(string callSid)
        {
            var call = GetPendingCall(callSid);
            if (call != null)
            {
                PendingCalls.Remove(call);
            }
        }

        /// <summary>
        /// Accepts a phone call and sets that call to the active phone call
        /// </summary>
        /// <param name="call"></param>
        public void AcceptCall(Call call)
        {
            RemovePendingCall(call.CallSid);
            ActiveCall = call;
        }

        /// <summary>
        /// Ends a phone call
        /// </summary>
        /// <param name="callSid">The phone call to be ended</param>
        public void EndCall(string callSid = null)
        {
            if (callSid != null)
            {
                RemovePendingCall(callSid);
            }

            if (ActiveCall != null && (callSid == null || ActiveCall.CallSid == callSid))
            {
                ActiveCall = null;
            }
        }
    }
}
