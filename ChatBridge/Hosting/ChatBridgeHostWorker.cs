using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatBridge.Hosting
{
    internal class ChatBridgeHostWorker : IHostedService
    {
        private ILogger<ChatBridgeHostWorker> _logger;
        private IServiceProvider _serviceProvider;
        public IReadOnlyCollection<IBridgeChat> Chats { get; }

        public ChatBridgeHostWorker(
            ILogger<ChatBridgeHostWorker> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            IEnumerable<IBridgeChat> chats = serviceProvider.GetServices<IBridgeChat>();
            if(chats.Count() < 1)
            {
                _logger.LogCritical("The bridge is meant to be run with ateast 2 Chats");
                throw new InvalidOperationException("The bridge is meant to be run with ateast 2 Chats");
            }

            Chats = new List<IBridgeChat>(chats);
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //Initialize part
            _logger.LogInformation("Start Initialize clients...");
            try
            {
                await Task.WhenAll(Chats.Select(x => x.InitializeAsync(cancellationToken)));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception occured while initializing Chats");
            }
            _logger.LogInformation("All clients Initialized successfully");


            _logger.LogInformation("Starting listening Chats");
            IEnumerable<IDisposable> activeSubs = null;
            try
            {
                IEnumerable<IObservable<BridgeMessage>> observables = await Task.WhenAll(Chats.Select(x => x.StartListenAsync(cancellationToken)));

                var observer = new BridgeWorkerObserver(Chats, _serviceProvider.GetService<ILogger<BridgeWorkerObserver>>());
                foreach(var idk in observables)
                {
                    idk.Subscribe(observer);
                }
                //activeSubs = observables.Select(x => x.Subscribe(observer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured while initializing Chats");
            }

            _logger.LogInformation("All clients Initialized successfully");
            _logger.LogInformation("Bridge is Active");

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    foreach(var sub in activeSubs)
                    {
                        sub.Dispose();
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, CancellationToken.None);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
        }

        private class BridgeWorkerObserver : IObserver<BridgeMessage>
        {
            private IReadOnlyCollection<IBridgeChat> _chats;
            private ILogger<BridgeWorkerObserver> _logger;

            public BridgeWorkerObserver(
                IReadOnlyCollection<IBridgeChat> chats,
                ILogger<BridgeWorkerObserver> logger)
            {
                _chats = chats;
                _logger = logger;
                _logger.LogInformation("Created observer");
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
                _logger.LogError(error, "Exception occured in receiving");
            }

            public void OnNext(BridgeMessage value)
            {
                _logger.LogInformation("received");
                //TODO: Maybe add lock?
                //IEnumerable<IBridgeChat> chatsToSend = _chats.Where(x => x != value.SourceChat);

                //Task.WhenAll(chatsToSend.Select(x => x.SendMessageAsync(value))).Wait();
                _logger.LogInformation($"Received message: {value.Sender}: {value.Content.First().GetDataAsync().Result.ToString()}");
            }
        }

    }
}
