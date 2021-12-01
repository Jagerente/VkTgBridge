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
                    receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                    {
                        new TextContent(update.Message.Text)
                    });
                    break;
                case MessageType.Sticker:
                    //Image Upload

                    var fileId = update.Message.Sticker.FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId);

                    if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                    var filePath = $"temp/{fileInfo.FilePath.Split("/").Last()}";

                    using (var fileStream = File.Create(filePath, 4096, FileOptions.DeleteOnClose))
                    {
                        await botClient.DownloadFileAsync(
                          fileInfo.FilePath,
                          fileStream
                        ); 

                        ISupportedImageFormat format = new PngFormat() { Quality = 70 };
                        using (MemoryStream inStream = new MemoryStream())
                        {
                            fileStream.Position = 0;
                            await fileStream.CopyToAsync(inStream);
                            using (MemoryStream outStream = new MemoryStream())
                            {
                                // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                                using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                                {
                                    //Size size = imageFactory.Load(inStream).Image.Size.Width == imageFactory.Load(inStream).Image.Size.Height ? new Size(150, 150) : imageFactory.Load(inStream).Image.Size;
                                    Size size = new Size(150, 150);
                                    // Load, resize, set the format and quality and save an image.
                                    imageFactory.Load(inStream)
                                        .Resize(size)
                                        .Format(format)
                                        .Save(outStream);
                                }
                                File.WriteAllBytes(filePath + ".png", outStream.GetBuffer());

                                var apiClient = new ApiClient("");
                                var httpClient = new HttpClient();
                                var imageEndpoint = new ImageEndpoint(apiClient, httpClient);
                                var imageUpload = await imageEndpoint.UploadImageAsync(outStream);
                                var url = imageUpload.Link;
                                receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                                {
                                            new StickerContent(url)
                                });

                                Console.WriteLine();
                                // Do something with the stream.
                                //File.Delete(filePath + ".webp");
                            }
                        }

                    }
                   
                    //var fileId = update.Message.Sticker.FileId;
                    //var fileInfo = await botClient.GetFileAsync(fileId);

                    //if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                    //Console.WriteLine(fileId);
                    //Console.WriteLine(fileInfo.FilePath);
                    //var filePath = $"temp/{fileInfo.FilePath.Split("/").Last()}";

                    //await using (var fileStream = File.Create(filePath, 4096, FileOptions.DeleteOnClose))
                    //{
                    //    await botClient.GetInfoAndDownloadFileAsync(fileId, fileStream);
                    //    fileStream.Position = 0;
                    //    // Format is automatically detected though can be changed.
                    //    ISupportedImageFormat format = new PngFormat() { Quality = 100 };
                    //    using (MemoryStream inStream = new MemoryStream())
                    //    {
                    //        using (MemoryStream outStream = new MemoryStream())
                    //        {
                    //            // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                    //            using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                    //            {
                    //                Size size = new Size(150, 150);
                    //                // Load, resize, set the format and quality and save an image.
                    //                imageFactory.Load(inStream)
                    //                    .Resize(size)
                    //                    .Format(format)
                    //                    .Save(outStream);
                    //            }
                    //            var apiClient = new ApiClient("");
                    //            var httpClient = new HttpClient();
                    //            var imageEndpoint = new ImageEndpoint(apiClient, httpClient);
                    //            var imageUpload = await imageEndpoint.UploadImageAsync(outStream);
                    //            var url = imageUpload.Link;
                    //            receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                    //            {
                    //                new StickerContent(url)
                    //            });
                    //        }
                    //    }
                    //}
                    break;

                case MessageType.Photo:
                    fileId = update.Message.Photo.LastOrDefault().FileId;
                    fileInfo = await botClient.GetFileAsync(fileId);
                    if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                    filePath = $"temp/{fileId}.{fileInfo.FilePath.Split(".").Last()}";
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
                case MessageType.Video:
                    var title = update.Message.Video.FileName;
                    fileId = update.Message.Video.Thumb.FileId;
                    fileInfo = await botClient.GetFileAsync(fileId);
                    if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                    filePath = $"temp/{fileId}.{fileInfo.FilePath.Split(".").Last()}";
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


            if (receivedMessage != null)
            {
                _messageSource.OnNext(receivedMessage);
            }
        }
    }
}
