using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.Caching.Memory
{
    internal class MessageMemoryCacheProvider : IMessageCacheProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageMemoryCacheProvider> _logger;
        private readonly SharedMessageMemoryCache _sharedCache;
        private readonly Dictionary<IBridgeChat, IMessageCache> _caches = new Dictionary<IBridgeChat, IMessageCache>();
        private readonly MessageMemoryCacheConfiguration _configuration;

        public MessageMemoryCacheProvider(
            ILogger<MessageMemoryCacheProvider> logger,
            IServiceProvider serviceProvider,
            MessageMemoryCacheConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _sharedCache = new SharedMessageMemoryCache(configuration);
        }

        private void InitializeCache()
        {
            if(_caches.Count == 0)
            {
                var chats = _serviceProvider.GetServices<IBridgeChat>();
                foreach(var chat in chats)
                {
                    _caches.Add(chat, new MessageMemoryCache(chat.ServiceName, _configuration, _sharedCache));
                    _logger.LogInformation($"Initialized MessageMemoryCache for ${chat.ServiceName}");
                }
            }
        }

        public IMessageCache GetCacheForService(string serviceName)
        {
            InitializeCache();
            return _caches.FirstOrDefault(x => x.Key.ServiceName == serviceName).Value;
        }
    }
}
