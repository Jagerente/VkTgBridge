using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge
{
    /// <summary>
    /// Base class for content of the message
    /// </summary>
    public abstract class BridgeMessageContent
    {
        /// <summary>
        /// Type of Content
        /// </summary>
        public BridgeMessageContentType Type { get; }

        protected BridgeMessageContent(BridgeMessageContentType type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets data of the Content
        /// </summary>
        /// <returns></returns>
        public abstract Task<object> GetDataAsync();
    }
}
