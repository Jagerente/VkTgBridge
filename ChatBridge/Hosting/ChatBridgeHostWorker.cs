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
    /// <summary>
    /// Worker of Bridge
    /// </summary>
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

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //Attempt to initialize all chats
            _logger.LogInformation("Start Initialize clients...");
            try
            {
                await Task.WhenAll(Chats.Select(x => x.InitializeAsync(cancellationToken)));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception occured while initializing Chats");
                throw;
            }
            _logger.LogInformation("All clients Initialized successfully");

            //Attempt to start all chats
            _logger.LogInformation("Starting listening Chats");
            List<IDisposable> activeSubs = new List<IDisposable>();
            try
            {
                //Observable sources to listen messages
                IEnumerable<IObservable<BridgeMessage>> observables = await Task.WhenAll(Chats.Select(x => x.StartListenAsync(cancellationToken)));

                var observer = new BridgeWorkerObserver(Chats, _serviceProvider.GetService<ILogger<BridgeWorkerObserver>>());
                foreach(var messageObservable in observables)
                {
                    //Subscribing observer to all sources of messages
                    IDisposable sub = messageObservable.Subscribe(observer);
                    activeSubs.Add(sub);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured while initializing Chats");
                throw;
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

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Special observer to track new <seealso cref="BridgeMessage"/>
        /// </summary>
        private class BridgeWorkerObserver : IObserver<BridgeMessage>
        {
            private IReadOnlyCollection<IBridgeChat> _chats;
            private ILogger<BridgeWorkerObserver> _logger;

            /// <summary>
            /// Creates new instance of observer
            /// </summary>
            /// <param name="chats">Active Chats</param>
            /// <param name="logger">Logger</param>
            public BridgeWorkerObserver(
                IReadOnlyCollection<IBridgeChat> chats,
                ILogger<BridgeWorkerObserver> logger)
            {
                _chats = chats;
                _logger = logger;
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
                //TODO: Maybe add lock?
                IEnumerable<IBridgeChat> chatsToSend = _chats.Where(x => x != value.SourceChat);
                //Makes all chats (exclusive source chat) send content
                Task.WhenAll(chatsToSend.Select(x => x.SendMessageAsync(value))).Wait();
            }
        }

    }
}
