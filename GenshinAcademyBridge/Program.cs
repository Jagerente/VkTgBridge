using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ChatBridge.Extensions.Vk;
using ChatBridge.Extensions.Telegram;
using ChatBridge.Hosting;
using GenshinAcademyBridge.Configuration;
using Serilog;

namespace GenshinAcademyBridge
{
    internal class Program
    {
        public static List<Bridge> Bridges;
        public static Dictionary<long, long> MessagesIds;

        //Configuration to pass as delegate for CreateDefaultHost of Bridge
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //Configuring logging to use Serilog further
            services.AddLogging(x =>
            {
                x.ClearProviders();
                x.AddSerilog();
            });
            //Adding VK Chat to bridge (Everything needed sich Random etc included there)
            services.AddVkChatBridge(configuration);
            //Adding Telegram Chat to bridge
            services.AddTelegramChatBridge(configuration);
        }

        public static async Task Main(string[] args)
        {
            //Creating hostbuilder for bridge
            var host = ChatBridgeHost.CreateDefaultHost(
                args,
                ConfigureServices,
                null)
                .UseSerilog((ctx, config) => //Using this to enable Serilog logger
                {
                    config
                        .MinimumLevel
                            .Information()
                        .WriteTo
                            .Console()
                        .WriteTo
                            .File("log.txt",
                                rollingInterval: RollingInterval.Day,
                                rollOnFileSizeLimit: true);
                })
                .Build();

            await host.RunAsync();
        }
    }
}
