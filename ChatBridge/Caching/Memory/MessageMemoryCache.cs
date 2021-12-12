using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.Caching.Memory
{
    internal class MessageMemoryCache : IMessageCache
    {
        private readonly SharedMessageMemoryCache _memoryCache;
        private readonly Dictionary<Guid, CacheKey> _guidToService;
        private object _locker = new object();
        private readonly Dictionary<object, Guid> _serviceToGuid;

        public readonly int MessageLimit;

        public string ServiceName { get; }

        public MessageMemoryCache(
            string service,
            MessageMemoryCacheConfiguration configuration,
            SharedMessageMemoryCache memoryCache)
        {
            ServiceName = service;
            MessageLimit = configuration.Limit;
            _serviceToGuid = new Dictionary<object, Guid>(MessageLimit);
            _guidToService = new Dictionary<Guid, CacheKey>(MessageLimit);
            _memoryCache = memoryCache;
        }

        private void CheckLimit()
        {
            lock (_locker)
            {
                if(_guidToService.Count == MessageLimit)
                {
                    KeyValuePair<Guid, CacheKey> item = _guidToService.First();
                    _guidToService.Remove(_guidToService.First().Key);
                    for(int i = 0; i < item.Value.Count; i++)
                    {
                        if (_serviceToGuid.ContainsKey(item.Value.GetKey(i)))
                        {
                            _serviceToGuid.Remove(item.Value.GetKey(i));
                        }
                    }
                    _memoryCache.RemoveIfNothingReferences(item.Key, ServiceName);
                }
            }
        }

        public void AddServiceId(BridgeMessage message, CacheKey serviceId)
        {
            lock (_locker)
            {
                CheckLimit();
                _guidToService.Add(message.BridgeId, serviceId);
                foreach(object key in serviceId.Keys)
                {
                    _serviceToGuid.Add(key, message.BridgeId);
                }
                _memoryCache.AddMessage(this, message);
                _memoryCache.AddReference(this, message.BridgeId);
            }
        }

        public bool TryGetMessageWithOffset(object id, out KeyValuePair<int, BridgeMessage>? messageCacheInfo)
        {
            lock (_locker)
            {
                if(_serviceToGuid.TryGetValue(id, out Guid guid))
                {
                    _memoryCache.TryGetMessage(guid, out BridgeMessage message);
                    CacheKey key = _guidToService[guid];
                    int index = key.FindKeyIndex(id);
                    messageCacheInfo = new KeyValuePair<int, BridgeMessage>(index, message);
                    return true;
                }
            }

            messageCacheInfo = null;
            return false;
        }

        public bool TryGetServiceId(Guid bridgeId, out CacheKey serviceId)
        {
            lock (_locker)
            {
                if (_guidToService.TryGetValue(bridgeId, out serviceId))
                {
                    return true;
                }

                serviceId = null;
                return false;
            }
        }
    }
}
