using GenshinAcademyBridge.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using GenshinAcademyBridge.Configuration;
using GenshinAcademyBridge.Modules;
using Microsoft.Extensions.Hosting;
using VkNet;
using System.Threading.Tasks;

namespace GenshinAcademyBridge
{
    class Program
    {
        public static List<Bridge> Bridges;

        public static Dictionary<long, long> MessagesIds;

        private static void ConfigureServices(IServiceCollection services)
        {
           var logger = new LoggerConfiguration()
             .MinimumLevel
             .Information()
             .WriteTo
             .Console()
             .WriteTo
             .File("log.txt",
                 rollingInterval: RollingInterval.Day,
                 rollOnFileSizeLimit: true)
             .CreateLogger();
            Log.Logger = logger;

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddSerilog(logger, true);
            });

            services.AddSingleton<VkApi>(provider => new VkApi(services));

            services.AddSingleton<VkBot>();
            services.AddSingleton<TgBot>();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    ConfigureServices(services);
                    services.Configure<HostOptions>(options =>
                    {
                        options.ShutdownTimeout = TimeSpan.MaxValue;
                    });
                    services.AddHostedService<ChatBridgeService>();
                });
        }

        public static async Task Main(string[] args)
        {
            MessagesIds = new Dictionary<long, long>();
            var host = CreateHostBuilder(args).Build();

            await host.RunAsync();
        }
    }
}
