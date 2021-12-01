using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents url containing only common text
    /// </summary>
    public class StickerContent : BridgeMessageContent
    {
        public readonly string Url;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain text as url
        /// </summary>
        /// <param name="url">Sticker URL</param>
        /// <param name="title">Video title</param>
        public StickerContent(string url) : base(BridgeMessageContentType.Sticker)
        {
            Url = url;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return Url;
        }
    }
}