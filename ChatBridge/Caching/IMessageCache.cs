using System;
using System.Collections.Generic;

namespace ChatBridge.Caching
{
    public interface IMessageCache
    {
        /// <summary>
        /// Associated service name
        /// </summary>
        string ServiceName { get; }

        void AddServiceId(BridgeMessage message, CacheKey serviceId);

        /// <summary>
        /// Attempts to get <seealso cref="BridgeMessage"/> by service id
        /// </summary>
        /// <param name="id">Service id</param>
        /// <param name="message">Result message</param>
        /// <returns>true if found; false otherwise</returns>
        bool TryGetMessageWithOffset(object id, out KeyValuePair<int, BridgeMessage>? messageCacheInfo);

        bool TryGetServiceId(Guid bridgeId, out CacheKey serviceId);
    }
}
