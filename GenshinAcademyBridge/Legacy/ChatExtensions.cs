using GenshinAcademyBridge.Configuration;
using GenshinAcademyBridge.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet;

namespace GenshinAcademyBridge
{
    public static class ChatExtensions
    {
        public static IServiceCollection AddVkChat(this IServiceCollection services, IConfiguration configuration)
        {
            var vkConfig = new VkConfiguration();
            configuration.Bind("Chats:Vk", vkConfig);
            services.AddSingleton(vkConfig);
            services.AddSingleton<IChat,VkBot>();

            return services;
        }

        public static IServiceCollection AddTelegramChat(this IServiceCollection services, IConfiguration configuration)
        {
            var tgConfig = new TgConfiguration();
            configuration.Bind("Chats:Telegram", tgConfig);
            services.AddSingleton(tgConfig);
            services.AddSingleton<IChat, TgBot>();

            return services;
        }
    }
}
