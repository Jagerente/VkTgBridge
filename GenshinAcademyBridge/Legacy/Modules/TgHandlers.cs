using GenshinAcademyBridge.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace GenshinAcademyBridge.Modules
{
    public class TgHandlers
    {
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Serilog.Log.Error(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            Serilog.Log.Information($"Receive message type: {message.Type}");
            string msg;
            string fileId;
            string filePath;
            Telegram.Bot.Types.File fileInfo;
            var sender = $"{message.From.Username.FirstCharToUpper()}";
            switch (message.Type)
            {
                case MessageType.Text:
                    //if (message.ReplyToMessage != null)
                    //{
                    //    msg = $"{message.From.Username.FirstCharToUpper()} reply to {message.ReplyToMessage.From.Username.FirstCharToUpper()} 💬\n";
                    //}

                    //Console.WriteLine(message.ForwardSenderName);

                    msg = Helpers.GetMessageTop(BridgeMessageType.Text, sender, message.Text);

                    //if (message.ForwardFrom != null)
                    //{
                    //    msg = Helpers.FormMessage(VkMessageType.Forwarded, sender, message.Caption, message.ForwardFrom.Username.FirstCharToUpper());
                    //}
                    //if (message.ForwardFromChat != null)
                    //{
                    //    Log.Logger.Information("Joined.");
                    //    msg = Helpers.FormMessage(VkMessageType.Forwarded, sender, message.Caption, message.ForwardFromChat.Username.FirstCharToUpper());
                    //}
                    //if (message.ReplyToMessage != null)
                    //{
                    //    Log.Logger.Information(message.ReplyToMessage.From.Username);
                    //    msg = Helpers.FormMessage(VkMessageType.Caption, sender, message.Caption, message.ReplyToMessage.From.Username).FirstCharToUpper();
                    //    foreach (var bridge in Program.Bridges)
                    //    {
                    //        Program.MessagesIds.Add(message.MessageId, await VkBot.ReplyAsync(bridge.VkId, msg, Program.MessagesIds[message.ReplyToMessage.MessageId]));
                    //    }
                    //    break;
                    //}

                    foreach (var bridge in Program.Bridges)
                    {
                        Program.MessagesIds.Add(message.MessageId, await VkBot.SendMessageAsync(bridge.VkId, msg));
                    }
                    break;
                case MessageType.Photo:
                    msg = Helpers.GetMessageTop(BridgeMessageType.Photo, sender, message.Caption);
                    fileId = message.Photo.LastOrDefault().FileId;
                    fileInfo = await botClient.GetFileAsync(fileId);
                    if (!Directory.Exists("resources")) Directory.CreateDirectory("resources");
                    filePath = $"resources/{fileId}.{fileInfo.FilePath.Split(".").Last()}";
                    using (var fileStream = System.IO.File.OpenWrite($"resources/{fileId}.{fileInfo.FilePath.Split(".").Last()}"))
                    {
                        await botClient.DownloadFileAsync(
                          filePath: fileInfo.FilePath,
                          destination: fileStream
                        );
                    }
                    foreach (var bridge in Program.Bridges)
                    {
                        await VkBot.SendPhotoAsync(bridge.VkId, msg, filePath);
                    }
                    break;
                case MessageType.Video:
                    msg = Helpers.GetMessageTop(BridgeMessageType.Video, sender, message.Caption, title: message.Video.FileName);
                    fileId = message.Video.Thumb.FileId;
                    fileInfo = await botClient.GetFileAsync(fileId);
                    if (!Directory.Exists("resources")) Directory.CreateDirectory("resources");
                    filePath = $"resources/{fileId}.{fileInfo.FilePath.Split(".").Last()}";
                    using (var fileStream = System.IO.File.OpenWrite($"resources/{fileId}.{fileInfo.FilePath.Split(".").Last()}"))
                    {
                        await botClient.DownloadFileAsync(
                          filePath: fileInfo.FilePath,
                          destination: fileStream
                        );
                    }
                    foreach (var bridge in Program.Bridges)
                    {
                        await VkBot.SendPhotoAsync(bridge.VkId, msg, filePath);
                    }
                    break;
                case MessageType.Sticker:
                    fileId = message.Sticker.FileId;
                    fileInfo = await botClient.GetFileAsync(fileId);
                    if (!Directory.Exists("resources")) Directory.CreateDirectory("resources");
                    filePath = $"resources/{fileId}";
                    using (var fileStream = System.IO.File.OpenWrite($"{filePath}.{fileInfo.FilePath.Split(".").Last()}"))
                    {
                        await botClient.DownloadFileAsync(
                          filePath: fileInfo.FilePath,
                          destination: fileStream
                        );
                    }
                    byte[] photoBytes = File.ReadAllBytes($"{filePath}.{fileInfo.FilePath.Split(".").Last()}");
                    // Format is automatically detected though can be changed.
                    ISupportedImageFormat format = new PngFormat(){ Quality = 70 };
                    using (MemoryStream inStream = new MemoryStream(photoBytes))
                    {
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
                            File.WriteAllBytes(filePath+".png", outStream.GetBuffer());
                            // Do something with the stream.
                            File.Delete(filePath + ".webp");
                        }
                    }

                    //using (Converter converter = new Converter($"{filePath}.{fileInfo.FilePath.Split(".").Last()}"))
                    //{
                    //    ImageConvertOptions options = new ImageConvertOptions
                    //    { 
                    //        Format = ImageFileType.Png,
                    //        Width = 125,
                    //        Height = 125
                    //    };
                    //    converter.Convert($"{filePath.Split(".").FirstOrDefault()}.png", options);
                    //}
                    foreach (var bridge in Program.Bridges)
                    {
                        await VkBot.SendStickerAsync(bridge.VkId, Helpers.GetMessageTop(BridgeMessageType.Sticker, sender), $"{filePath}.png");
                    }
                    break;
                case MessageType.Poll:
                    foreach (var bridge in Program.Bridges)
                    {
                        await VkBot.SendMessageAsync(bridge.VkId, Helpers.GetMessageTop(BridgeMessageType.Poll, sender));
                        await VkBot.SendPollAsync(bridge.VkId, message.Poll.Question, message.Poll.Options.Select(x => x.Text).ToArray(), message.Poll.IsAnonymous, message.Poll.AllowsMultipleAnswers);
                    }
                    break;
                default:
                    return;
            }
        }

        // Process Inline Keyboard callback data
        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}
