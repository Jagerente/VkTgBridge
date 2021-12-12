using System;
using Microsoft.Extensions.DependencyInjection;

namespace ChatBridge.Caching.Memory
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddMemoryCache(this IServiceCollection services, Action<MessageMemoryCacheConfiguration> config = null)
        {
            var configuration = new MessageMemoryCacheConfiguration()
            {
                Limit = 500
            };
            if(config != null)
            {
                config(configuration);
            }

            services.AddSingleton(configuration);
            services.AddSingleton<IMessageCacheProvider, MessageMemoryCacheProvider>();

            return services;
        }
    }
}
