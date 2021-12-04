using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents content containing only photo
    /// </summary>
    public class PhotoContent : BridgeMessageContent
    {
        public readonly string Url;
        public readonly string Caption;
        public readonly string Sender;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain caption as url
        /// </summary>
        /// <param name="url">Photo URL</param>
        /// <param name="caption">Appended caption</param>
        /// <param name="sender">Sender name</param>
        public PhotoContent(string url, string caption, string sender) : base(BridgeMessageContentType.Photo)
        {
            Url = url;
            Caption = caption;
            Sender = sender;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return this;
        }

    }
}
