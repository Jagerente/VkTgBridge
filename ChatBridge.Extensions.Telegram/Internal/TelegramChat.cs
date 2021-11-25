﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Extensions;
using System.Reactive.Subjects;

namespace ChatBridge.Extensions.Telegram.Internal
{
    internal class TelegramChat : IBridgeChat
    {
        private User _botUser;
        private readonly TelegramBotClient _client;
        private readonly TelegramChatConfiguration _configuration;
        private readonly ILogger<TelegramChat> _logger;
        private Chat _chat;

        public string ServiceName => "Telegram";

        public TelegramChat(
            ILogger<TelegramChat> logger,
            TelegramChatConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _client = new TelegramBotClient(configuration.Token);
        }

        public async Task InitializeAsync(CancellationToken cancelToken = default)
        {
            _logger.LogInformation("Start initializing Telegram chat...");
            _botUser = await _client.GetMeAsync(cancelToken);

            _chat = await _client.GetChatAsync(new ChatId(_configuration.ChatId.Value), cancelToken);

            _logger.LogInformation($"Telegram chat initialized successfully as {_botUser.Username}:{_botUser.Id}");
        }

        public async Task SendMessageAsync(BridgeMessage message, CancellationToken cancelToken = default)
        {
            var content = (string) await message.FirstOrDefault().GetDataAsync();
            await _client.SendTextMessageAsync(new ChatId(_configuration.ChatId.Value), content);
        }

        public async Task<IObservable<BridgeMessage>> StartListenAsync(CancellationToken cancelToken = default)
        {
            await Task.CompletedTask;

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }, // Receiving messages only
                ThrowPendingUpdates = true
            };
            var subject = new Subject<BridgeMessage>();
            var updateHandler = new TelegramChatUpdateHandler(this, subject, _configuration.AllowMessageTypes);

            _client.StartReceiving(
                updateHandler,
                receiverOptions,
                cancelToken);

            _logger.LogInformation("Telegram chat started listening...");
            return subject;
        }
    }
}