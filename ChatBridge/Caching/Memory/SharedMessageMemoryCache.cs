using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.Caching.Memory
{
    internal class SharedMessageMemoryCache
    {
        public readonly int MessageLimit;

        private readonly Dictionary<Guid, BridgeMessage> _messageCache;
        private readonly Dictionary<Guid, List<string>> _messageReferences = new Dictionary<Guid, List<string>>();

        public SharedMessageMemoryCache(MessageMemoryCacheConfiguration configuration)
        {
            MessageLimit = configuration.Limit;
        }

        public void AddReference(IMessageCache cache, Guid messageId)
        {
            List<string> references = _messageReferences[messageId];
            if (references.Contains(cache.ServiceName))
            {
                return;
            }
            references.Add(cache.ServiceName);
        }

        public void AddMessage(IMessageCache cache, BridgeMessage message)
        {
            if (!_messageCache.ContainsKey(message.BridgeId))
            {
                _messageCache.Add(message.BridgeId, message);
                _messageReferences.Add(message.BridgeId, new List<string>(new string[] { cache.ServiceName }));
                return;
            }
        }

        public bool TryGetMessage(Guid messageId, out BridgeMessage message)
        {
            if(_messageCache.TryGetValue(messageId, out message))
            {
                return true;
            }
            message = null;
            return false;
        }

        public void RemoveIfNothingReferences(Guid messageId, string serviceName)
        {
            List<string> references = _messageReferences[messageId];
            if (references.Contains(serviceName))
            {
                references.Remove(serviceName);
                if(references.Count == 0)
                {
                    _messageCache.Remove(messageId);
                    _messageReferences.Remove(messageId);
                }
            }
        }
    }
}
