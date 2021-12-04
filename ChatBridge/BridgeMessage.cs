using System;
using System.Collections;
using System.Collections.Generic;

namespace ChatBridge
{
    /// <summary>
    /// Represents message inside the Bridge infrastructure
    /// </summary>
    public class BridgeMessage : IEnumerable<BridgeMessageContent>
    {
        /// <summary>
        /// Id for message used in Bridge infrastructure
        /// </summary>
        public Guid BridgeId { get; } = Guid.NewGuid();

        /// <summary>
        /// Text of Message
        /// </summary>
        public IEnumerable<BridgeMessageContent> Content { get; }

        /// <summary>
        /// Name of who sent the message
        /// </summary>
        public string Sender { get; }

        /// <summary>
        /// Chat received message
        /// </summary>
        public IBridgeChat SourceChat { get; }

        public BridgeMessage(IBridgeChat source, string sender, IEnumerable<BridgeMessageContent> content)
        {
            if (string.IsNullOrWhiteSpace(sender))
            {
                throw new ArgumentNullException(nameof(sender));
            }

            Sender = sender;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            SourceChat = source;
        }

        /// <inheritdoc/>
        public IEnumerator<BridgeMessageContent> GetEnumerator()
        {
            return Content.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Content.GetEnumerator();
        }
    }
}
