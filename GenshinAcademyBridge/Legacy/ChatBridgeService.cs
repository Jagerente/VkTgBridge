using GenshinAcademyBridge.Configuration;
using GenshinAcademyBridge.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

using System.Reactive.Linq;

namespace GenshinAcademyBridge
{
    public class ChatBridgeService : IHostedService
    {
        private ILogger _logger;

        public const string ConfigPath = "configuration/";
        public const string BridgesPath = ConfigPath + "bridges/";

        public IReadOnlyCollection<IChat> Chats { get; }

        public ChatBridgeService(ILogger logger, IServiceProvider serviceProvider)
        {
            IEnumerable<IChat> chatServices = serviceProvider.GetServices<IChat>();
            if(chatServices.Count() <= 1)
            {
                throw new InvalidOperationException("Application is ment to be run with ateast 2 chats");
            }
            _logger = logger;
            SetupBridges();
            Chats = new List<IChat>(chatServices);
        }


        private static void SetupBridges()
        {
            if (!Directory.Exists(BridgesPath)) Directory.CreateDirectory(BridgesPath);

            if (Directory.GetFiles(BridgesPath).Length == 0)
            {
                var cfg = new Bridge();
                Console.WriteLine("Set bridge title:");
                var title = Console.ReadLine();
                Console.WriteLine("Set VK conversation id:");
                cfg.VkId = long.Parse(Console.ReadLine() ?? string.Empty);
                Console.WriteLine("Set TG conversation id:");
                cfg.TgId = long.Parse(Console.ReadLine() ?? string.Empty);
                JsonStorage.StoreObject(cfg, $"{BridgesPath}/{title}.json");
            }
            Program.Bridges = new List<Bridge>();
            foreach (var bridge in Directory.GetFiles(BridgesPath))
            {
                Program.Bridges.Add(JsonStorage.RestoreObject<Bridge>(bridge));
            }
        }

        private class MyObserver : IObserver<TextMessage>
        {
            public void OnCompleted()
            {

            }

            public void OnError(Exception error)
            {
                Console.WriteLine("Error");
                throw error;
            }

            public void OnNext(TextMessage value)
            {
                Console.WriteLine($"Received message; {value.Sender} - {value.Message}");
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(Chats.Select(x => x.InitializeAsync()));
            IEnumerable<IObservable<TextMessage>> observables = await Task.WhenAll(Chats.Select(x => x.StartListenAsync()));
            var idk = Observable.Concat(observables);
            var sub = idk.Subscribe(new MyObserver());
            _logger.Information("Application started.");
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    sub.Dispose();
                }
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
