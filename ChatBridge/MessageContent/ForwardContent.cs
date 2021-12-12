using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents content containing only photo
    /// </summary>
    public class ForwardContent : BridgeMessageContent
    {
        public readonly List<BridgeMessageContent> Messages;
        public readonly string Sender;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain caption as url
        /// </summary>
        /// <param name="messages">Forwarded messages</param>
        /// <param name="sender">Sender name</param>
        public ForwardContent(List<BridgeMessageContent> messages, string sender) : base(BridgeMessageContentType.Forwarded)
        {
            Messages = messages;
            Sender = sender;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return this;
        }

    }
}
