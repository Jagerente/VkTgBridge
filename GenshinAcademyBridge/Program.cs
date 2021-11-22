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
using System.Linq;

namespace GenshinAcademyBridge
{
    class Program
    {
        public static List<Bridge> Bridges;

        public static Dictionary<long, long> MessagesIds;

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(services);
            services.AddSingleton(new Random());

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
            services.AddSingleton(Log.Logger);

            services.AddVkChat(configuration);
            services.AddTelegramChat(configuration);
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IConfiguration config = null;
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    builder.Sources.Clear();

                    builder.AddJsonFile("bridgeConfiguration.json");
                    builder.AddEnvironmentVariables();
                    if(args.Length > 0)
                    {
                        builder.AddCommandLine(args);
                    }

                    config = builder.Build();
                })
                .ConfigureServices(services =>
                {
                    ConfigureServices(services, config);
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
