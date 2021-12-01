using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain caption as url
        /// </summary>
        /// <param name="url">Photo URL</param>
        /// <param name="caption">Appended caption</param>
        public PhotoContent(string url, string caption) : base(BridgeMessageContentType.Photo)
        {
            Url = url;
            Caption = caption;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return this;
        }

    }
}
