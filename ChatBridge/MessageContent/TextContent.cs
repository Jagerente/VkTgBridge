using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents content containing text only
    /// </summary>
    public class TextContent : BridgeMessageContent
    {
        public readonly string Text;
        public readonly string Sender;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain content as text
        /// </summary>
        /// <param name="text">Text of the message</param>
        /// <param name="sender">Sender name</param>
        public TextContent(string text, string sender) : base(BridgeMessageContentType.Text)
        {
            Text = text;
            Sender = sender;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return Text;
        }
    }
}
