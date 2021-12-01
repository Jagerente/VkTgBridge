using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents content containing text only
    /// </summary>
    public class TextContent : BridgeMessageContent
    {
        public readonly string Text;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain content as text
        /// </summary>
        /// <param name="text">Text of the message</param>
        public TextContent(string text) : base(BridgeMessageContentType.Text)
        {
            Text = text;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return Text;
        }
    }
}
