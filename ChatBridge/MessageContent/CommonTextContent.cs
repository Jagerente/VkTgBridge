using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents content containing only common text
    /// </summary>
    public class CommonTextContent : BridgeMessageContent
    {
        private readonly string _content;

        public CommonTextContent(string content) : base(BridgeMessageContentType.Text)
        {
            _content = content;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return _content;
        }
    }
}
