using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents url containing only common text
    /// </summary>
    public class StickerContent : BridgeMessageContent
    {
        public readonly string Url;
        public readonly string Sender;
        public readonly byte[] File;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain text as url
        /// </summary>
        /// <param name="url">Sticker URL</param>
        /// <param name="sender">Sender name</param>
        public StickerContent(string url, string sender) : base(BridgeMessageContentType.Sticker)
        {
            Url = url;
            Sender = sender;
        }

        public StickerContent(byte[] file, string sender) : base(BridgeMessageContentType.Sticker)
        {
            File = file;
            Sender = sender;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return Url;
        }
    }
}