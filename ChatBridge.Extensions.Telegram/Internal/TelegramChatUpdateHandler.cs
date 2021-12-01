using ChatBridge.MessageContent;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using Imgur.API.Authentication;
using Imgur.API.Endpoints;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace ChatBridge.Extensions.Telegram.Internal
{
    /// <summary>
    /// Listener of Updates for <seealso cref="TelegramChat"/>
    /// </summary>
    internal class TelegramChatUpdateHandler : IUpdateHandler
    {
        private readonly TelegramChat _chat;
        private readonly Subject<BridgeMessage> _messageSource;
        private readonly IReadOnlyCollection<BridgeMessageContentType> _contentTypes;

        /// <summary>
        /// Creates new instance of UpdateHandler
        /// </summary>
        /// <param name="chat">Chat this handler belongs to</param>
        /// <param name="subject">Subject that issues messages</param>
        /// <param name="allowedTypes">Allowed/Supported types to handle</param>
        public TelegramChatUpdateHandler(
            TelegramChat chat,
            Subject<BridgeMessage> subject,
            IEnumerable<BridgeMessageContentType> allowedTypes)
        {
            _chat = chat;
            _messageSource = subject;
            _contentTypes = allowedTypes == null ? new List<BridgeMessageContentType>() : new List<BridgeMessageContentType>(allowedTypes);
        }

        /// <inheritdoc/>
        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _messageSource.OnError(exception);
        }

        /// <inheritdoc/>
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            string senderName = update.Message.From.Username ?? $"{update.Message.From.FirstName} {update.Message.From.LastName}";
            BridgeMessage receivedMessage = null;

            switch (update.Message.Type)
            {
                case MessageType.Text:
                {
                    receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                    {
                        new TextContent(update.Message.Text)
                    });
                    break;
                }
                case MessageType.Sticker:
                {
                    await using (var fileStream = new MemoryStream())
                    {
                        var fileId = update.Message.Sticker.FileId;

                        await botClient.GetInfoAndDownloadFileAsync(fileId, fileStream);

                        fileStream.Position = 0L;
                        ISupportedImageFormat format = new PngFormat() {Quality = 70};
                        await using (MemoryStream outStream = new MemoryStream())
                        {
                            // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                            using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                            {
                                Size size = new Size(150, 150);
                                // Load, resize, set the format and quality and save an image.
                                imageFactory.Load(fileStream)
                                    .Resize(size)
                                    .Format(format)
                                    .Save(outStream);
                            }

                            var apiClient = new ApiClient("e85b52d0b7d494f");
                            var httpClient = new HttpClient();
                            var imageEndpoint = new ImageEndpoint(apiClient, httpClient);
                            var imageUpload = await imageEndpoint.UploadImageAsync(outStream);
                            var url = imageUpload.Link;
                            receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                            {
                                new StickerContent(url)
                            });

                        }
                    }


                    break;
                }
                case MessageType.Photo:
                {
                    var fileId = update.Message.Photo.LastOrDefault().FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId);
                    if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                    var filePath = $"temp/{fileId}.{fileInfo.FilePath.Split(".").Last()}";
                    await using (var fileStream = System.IO.File.Create(filePath, 4096, FileOptions.DeleteOnClose))
                    {
                        await botClient.GetInfoAndDownloadFileAsync(fileId, fileStream);

                        var apiClient = new ApiClient("");
                        var httpClient = new HttpClient();
                        var imageEndpoint = new ImageEndpoint(apiClient, httpClient);
                        var imageUpload = await imageEndpoint.UploadImageAsync(fileStream);

                        var url = imageUpload.Link;

                        receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                        {
                            new PhotoContent(url, update.Message.Caption)
                        });
                    }

                    break;
                }
                case MessageType.Video:
                {
                    var title = update.Message.Video.FileName;
                    var fileId = update.Message.Video.Thumb.FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId);
                    if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                    var filePath = $"temp/{fileId}.{fileInfo.FilePath.Split(".").Last()}";
                    await using (var fileStream = System.IO.File.Create(filePath, 4096, FileOptions.DeleteOnClose))
                    {
                        await botClient.GetInfoAndDownloadFileAsync(fileId, fileStream);

                        var apiClient = new ApiClient("");
                        var httpClient = new HttpClient();
                        var imageEndpoint = new ImageEndpoint(apiClient, httpClient);
                        var imageUpload = await imageEndpoint.UploadImageAsync(fileStream);

                        var url = imageUpload.Link;

                        receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                        {
                            new VideoContent(url, title, update.Message.Caption)
                        });
                    }

                    break;
                }
            }


            if (receivedMessage != null)
            {
                _messageSource.OnNext(receivedMessage);
            }
        }
    }
}
