using ChatBridge.Extensions.Telegram.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.Extensions.Telegram
{
    public static class ChatBridgeExtensions
    {
        private static void EnsureConfigIsFull(TelegramChatConfiguration config)
        {
            if (config.ChatId == null)
            {
                throw new ArgumentNullException(nameof(TelegramChatConfiguration.ChatId));
            }
            if (config.Token == null)
            {
                throw new ArgumentNullException(nameof(TelegramChatConfiguration.Token));
            }
        }

        /// <summary>
        /// Adds chat for Telegram
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Configuration for Chat</param>
        /// <returns></returns>
        public static IServiceCollection AddTelegramChatBridge(
            this IServiceCollection services,
            Action<TelegramChatConfiguration> config)
        {
            TelegramChatConfiguration tgConfig = new TelegramChatConfiguration();
            config.Invoke(tgConfig);
            EnsureConfigIsFull(tgConfig);

            services.AddSingleton<TelegramChatConfiguration>(tgConfig);
            services.AddSingleton<IBridgeChat, TelegramChat>();

            return services;
        }


        /// <summary>
        /// Adds chat for Telegram
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Configuration to configure from</param>
        /// <returns></returns>
        /// <remarks>Should follow bridgeConfiguration.json strucure</remarks>
        public static IServiceCollection AddTelegramChatBridge(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            TelegramChatConfiguration tgConfig = new TelegramChatConfiguration();
            configuration.Bind("Chats:Telegram", tgConfig);
            EnsureConfigIsFull(tgConfig);

            services.AddSingleton<TelegramChatConfiguration>(tgConfig);
            services.AddSingleton<IBridgeChat, TelegramChat>();

            return services;
        }
    }
}
