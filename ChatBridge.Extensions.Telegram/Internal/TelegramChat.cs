using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatBridge.MessageContent;
using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ChatBridge.Extensions.Telegram.Internal
{
    /// <summary>
    /// Default Telegram implementation of <seealso cref="IBridgeChat"/>
    /// </summary>
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

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancelToken = default)
        {
            _logger.LogInformation("Start initializing Telegram chat...");
            _botUser = await _client.GetMeAsync(cancelToken);

            _chat = await _client.GetChatAsync(new ChatId(_configuration.ChatId.Value), cancelToken);

            _logger.LogInformation($"Telegram chat initialized successfully as {_botUser.Username}:{_botUser.Id}");
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(BridgeMessage message, CancellationToken cancelToken = default)
        {
            //var content = (string) await message.FirstOrDefault().GetDataAsync();
            foreach (var content in message.Content)
            {
                switch (content.Type)
                {
                    case BridgeMessageContentType.Unknown:
                        break;
                    case BridgeMessageContentType.Text:
                        await SendMessageAsync(_configuration.ChatId.Value, content.AsTextContent().Text);
                        break;
                    case BridgeMessageContentType.Photo:
                        await SendPhotoAsync(_configuration.ChatId.Value, content.AsPhotoContent().Caption, content.AsPhotoContent().Url);
                        break;
                    case BridgeMessageContentType.Audio:
                        break;
                    case BridgeMessageContentType.Video:
                        await SendVideoAsync(_configuration.ChatId.Value, content.AsVideoContent().Caption, content.AsVideoContent().Thumbnail);
                        break;
                    case BridgeMessageContentType.Voice:
                        break;
                    case BridgeMessageContentType.Document:
                        break;
                    case BridgeMessageContentType.Sticker:
                        await SendStickerAsync(_configuration.ChatId.Value, string.Empty, content.AsStickerContent().Url);
                        break;
                    case BridgeMessageContentType.ChatMembersAdded:
                        break;
                    case BridgeMessageContentType.ChatMemberLeft:
                        break;
                    case BridgeMessageContentType.Poll:
                        await SendPollAsync(_configuration.ChatId.Value, content.AsPollContent().Question, content.AsPollContent().Options, content.AsPollContent().IsAnonymous, content.AsPollContent().IsMultiple);
                        break;
                    case BridgeMessageContentType.Reply:
                        break;
                    case BridgeMessageContentType.Forwarded:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public async Task<long> SendMessageAsync(long conversationId, string message)
        {
            return Convert.ToInt64((await _client.SendTextMessageAsync(chatId: conversationId, text: message)).MessageId);
        }

        public async Task<long> ReplyAsync(long conversationId, string message, long id)
        {
            return Convert.ToInt64((await _client.SendTextMessageAsync(chatId: conversationId, text: message, replyToMessageId: Convert.ToInt32(id))).MessageId);
        }

        public async Task SendStickerAsync(long conversationId, string message, string url)
        {
            await SendPhotoAsync(conversationId, message, url);
        }

        public async Task SendPhotoAsync(long conversationId, string message, string url)
        {
            await _client.SendPhotoAsync(chatId: conversationId, photo: url, caption: message);
        }

        public async Task SendPhotoAsync(long conversationId, string message, string[] url)
        {
            if (url.Length == 1)
            {
                await SendPhotoAsync(conversationId, message, url.FirstOrDefault());
            }
            else
            {
                var photos = new IAlbumInputMedia[] { };
                foreach (var photo in url)
                {
                    photos = photos.Append(new InputMediaPhoto(photo)).ToArray();
                }
                await _client.SendMediaGroupAsync(chatId: conversationId, media: photos);
                await SendMessageAsync(conversationId, message);
            }
        }

        public async Task SendVideoAsync(long conversationId, string message, string url)
        { 
            await _client.SendPhotoAsync(chatId: conversationId, photo: url, caption: message);
        }

        public async Task SendPollAsync(long conversationId, string question, string[] options, bool? isAnonymous, bool? allowsMultipleAnswers)
        {
            await _client.SendPollAsync(conversationId, $"{question}", options, isAnonymous, allowsMultipleAnswers: allowsMultipleAnswers);

        }


        /// <inheritdoc/>
        public async Task<IObservable<BridgeMessage>> StartListenAsync(CancellationToken cancelToken = default)
        {
            await Task.CompletedTask;

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }, // Receiving messages only
                ThrowPendingUpdates = true
            };
            var subject = new Subject<BridgeMessage>();
            //Creating handler
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
