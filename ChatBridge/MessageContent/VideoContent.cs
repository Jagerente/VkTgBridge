using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain text as thumbnail
        /// </summary>
        /// <param name="thumbnail">Video preview URL</param>
        /// <param name="title">Video title</param>
        /// <param name="caption">Appended caption</param>
        public VideoContent(string thumbnail, string title, string caption) : base(BridgeMessageContentType.Video)
        {
            Thumbnail = thumbnail;
            Title = title;
            Caption = caption;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return Thumbnail;
        }
    }
}