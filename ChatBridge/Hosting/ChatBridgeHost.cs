using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.Hosting
{
    /// <summary>
    /// Provides ways to create redy-to-use <seealso cref="IHostBuilder"/> for Bridge to work out of box
    /// </summary>
    public static class ChatBridgeHost
    {
        /// <summary>
        /// Creates default <seealso cref="IHostBuilder"/> to run Bridge
        /// </summary>
        /// <param name="configure">Services configuration</param>
        /// <param name="timeout">Shutdown timeout of Bridge</param>
        /// <returns></returns>
        public static IHostBuilder CreateDefaultHost(
            Action<IServiceCollection, IConfiguration> configure,
            TimeSpan? timeout = null)
        {
            return CreateDefaultHost(Array.Empty<string>(), configure, timeout);
        }

        /// <summary>
        /// Creates default <seealso cref="IHostBuilder"/> to run Bridge
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <param name="configure">Services configuration</param>
        /// <param name="timeout">Shutdown timeout of Bridge</param>
        /// <returns></returns>
        public static IHostBuilder CreateDefaultHost(
            string[] args,
            Action<IServiceCollection, IConfiguration> configure,
            TimeSpan? timeout = null)
        {
            IConfiguration configuration = null;
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((builder) =>
                {
                    builder.Sources.Clear();

                    builder.AddJsonFile("bridgeConfiguration.json");
                    builder.AddEnvironmentVariables();
                    if (args.Length > 0)
                    {
                        builder.AddCommandLine(args);
                    }

                    configuration = builder.Build();
                })
                .ConfigureServices(services =>
                {
                    configure(services, configuration);
                    services.Configure<HostOptions>(options =>
                    {
                        options.ShutdownTimeout = timeout ?? TimeSpan.MaxValue;
                    });
                    services.AddHostedService<ChatBridgeHostWorker>();
                });
        }
    }
}
