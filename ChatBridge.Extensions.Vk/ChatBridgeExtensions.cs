using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ChatBridge.Extensions.Vk.Internal;

namespace ChatBridge.Extensions.Vk
{
    public static class ChatBridgeExtensions
    {
        private static void EnsureConfigIsFull(VkChatConfiguration config)
        {
            if(config.ChatId == null)
            {
                throw new ArgumentNullException(nameof(VkChatConfiguration.ChatId));
            }
            if (config.GroupId == null)
            {
                throw new ArgumentNullException(nameof(VkChatConfiguration.GroupId));
            }
            if (config.Token == null)
            {
                throw new ArgumentNullException(nameof(VkChatConfiguration.Token));
            }
        }

        /// <summary>
        /// Adds chat for Vk (https://vk.com)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Configuration for Chat</param>
        /// <returns></returns>
        public static IServiceCollection AddVkChatBridge(
            this IServiceCollection services,
            Action<VkChatConfiguration> config)
        {
            VkChatConfiguration vkConfig = new VkChatConfiguration();
            config.Invoke(vkConfig);
            EnsureConfigIsFull(vkConfig);

            services.AddSingleton(services);
            services.AddSingleton<VkChatConfiguration>(vkConfig);
            services.AddSingleton(new Random());
            services.AddSingleton<IBridgeChat, VkChat>();

            return services;
        }


        /// <summary>
        /// Adds chat for Vk (https://vk.com)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Configuration to configure from</param>
        /// <returns></returns>
        /// <remarks>Should follow bridgeConfiguration.json strucure</remarks>
        public static IServiceCollection AddVkChatBridge(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            VkChatConfiguration vkConfig = new VkChatConfiguration();
            configuration.Bind("Chats:Vk", vkConfig);
            EnsureConfigIsFull(vkConfig);

            services.AddSingleton(services);
            services.AddSingleton<VkChatConfiguration>(vkConfig);
            services.AddSingleton(new Random());
            services.AddSingleton<IBridgeChat, VkChat>();

            return services;
        }
    }
}
