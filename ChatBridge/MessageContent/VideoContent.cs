using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents thumbnail containing only common text
    /// </summary>
    public class VideoContent : BridgeMessageContent
    {
        public readonly string Thumbnail;
        public readonly string Title;
        public readonly string Caption;
        public readonly string Sender;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain text as thumbnail
        /// </summary>
        /// <param name="thumbnail">Video preview URL</param>
        /// <param name="title">Video title</param>
        /// <param name="caption">Appended caption</param>
        /// <param name="sender">Sender name</param>
        public VideoContent(string thumbnail, string title, string caption, string sender) : base(BridgeMessageContentType.Video)
        {
            Thumbnail = thumbnail;
            Title = title;
            Caption = caption;
            Sender = sender;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return Thumbnail;
        }
    }
}