using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ChatBridge.MessageContent;
using Microsoft.Extensions.Logging;

using Telegram.Bot;
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
                        await SendTextAsync(_configuration.ChatId.Value, content.AsTextContent());
                        break;
                    case BridgeMessageContentType.Photo:
                        await SendPhotoAsync(_configuration.ChatId.Value, content.AsPhotoContent());
                        break;
                    case BridgeMessageContentType.Audio:
                        break;
                    case BridgeMessageContentType.Video:
                        await SendVideoAsync(_configuration.ChatId.Value, content.AsVideoContent());
                        break;
                    case BridgeMessageContentType.Voice:
                        break;
                    case BridgeMessageContentType.Document:
                        break;
                    case BridgeMessageContentType.Sticker:
                        await SendStickerAsync(_configuration.ChatId.Value, content.AsStickerContent());
                        break;
                    case BridgeMessageContentType.ChatMembersAdded:
                        break;
                    case BridgeMessageContentType.ChatMemberLeft:
                        break;
                    case BridgeMessageContentType.Poll:
                        await SendPollAsync(_configuration.ChatId.Value, content.AsPollContent());
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

        public async Task<long> SendTextAsync(long conversationId, TextContent content)
        {
            return Convert.ToInt64((await _client.SendTextMessageAsync(conversationId, content.FormMessage())).MessageId);
        }

        //public async Task<long> ReplyAsync(long conversationId, string message)
        //{
        //    return Convert.ToInt64((await _client.SendTextMessageAsync(chatId: conversationId, text: message, replyToMessageId: Convert.ToInt32(id))).MessageId);
        //}

        public async Task SendStickerAsync(long conversationId, StickerContent content)
        {
            await using (var fileStream = new MemoryStream(content.File))
            {
                await _client.SendTextMessageAsync(conversationId, content.FormMessage());
                await _client.SendStickerAsync(conversationId, fileStream);

            }
        }

        public async Task SendPhotoAsync(long conversationId, PhotoContent content)
        {
            await SendTextAsync(conversationId,
                new TextContent(content.FormMessage(), content.Sender));
            await _client.SendPhotoAsync(conversationId, content.Url, content.FormMessage());
        }

        public async Task SendPhotosAsync(long conversationId, PhotoContent[] contents)
        {
            var photos = new IAlbumInputMedia[] { };

            photos = contents.Aggregate(photos, (current, content) => current.Append(new InputMediaPhoto(content.Url)).ToArray());

            await _client.SendMediaGroupAsync(chatId: conversationId, media: photos);
            if (!string.IsNullOrEmpty(contents.FirstOrDefault().Caption))
            {
                await SendTextAsync(conversationId,
                    new TextContent(contents.FirstOrDefault().FormMessage(), contents.FirstOrDefault().Sender));
            }
        }

        public async Task SendVideoAsync(long conversationId, VideoContent content)
        { 
            await _client.SendPhotoAsync(conversationId, content.Thumbnail, content.FormMessage());
        }

        public async Task SendPollAsync(long conversationId, PollContent content)
        { 
            await _client.SendTextMessageAsync(conversationId, content.FormMessage());
            await _client.SendPollAsync(conversationId, content.Question, content.Options, content.IsAnonymous, allowsMultipleAnswers: content.IsMultiple);
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
