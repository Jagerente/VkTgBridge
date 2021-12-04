using System.Threading.Tasks;

namespace ChatBridge
{
    /// <summary>
    /// Base class for content of the message
    /// </summary>
    public abstract class BridgeMessageContent
    {
        /// <summary>
        /// Type of Text
        /// </summary>
        public BridgeMessageContentType Type { get; }

        protected BridgeMessageContent(BridgeMessageContentType type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets data of the Text
        /// </summary>
        /// <returns></returns>
        public abstract Task<object> GetDataAsync();
    }
}
