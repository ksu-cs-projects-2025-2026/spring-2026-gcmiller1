using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentView
{
    public class CommService
    {
        private ClientWebSocket agentws = new();
        public WebSocketState State => agentws.State;

        /// <summary>
        /// Connects to the server at a given endpoint
        /// </summary>
        /// <param name="url">the url to be connected to</param>
        /// <returns></returns>
        public async Task ConnectAsync(string url)
        {
            await agentws.ConnectAsync(new Uri(url), CancellationToken.None);
        }

        /// <summary>
        /// Sends a json message to the server
        /// </summary>
        /// <param name="payload">the message to be sent</param>
        /// <returns></returns>
        public async Task SendJsonAsync(object payload)
        {
            if (agentws.State != WebSocketState.Open)
            {
                return;
            }

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            await agentws.SendAsync( new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Listens for messages from the server and directs them according to the message type
        /// </summary>
        /// <param name="onText">where to handle text messages</param>
        /// <param name="onBinary">where to handle binary messages</param>
        /// <returns></returns>
        public async Task ReceiveLoopAsync(Func<string, Task> onText, Func<byte[], Task> onBinary)
        {
            var buffer = new byte[8192];

            while (agentws.State == WebSocketState.Open)
            {
                var result = await agentws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await onText(json);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var bytes = buffer.Take(result.Count).ToArray();
                    await onBinary(bytes);
                }
            }
        }
    }
}
