using GenshinAcademyBridge.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

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

            Console.WriteLine(ErrorMessage);
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
                    if (message.ReplyToMessage != null)
                    {
                        msg = $"{message.From.Username.FirstCharToUpper()} reply to {message.ReplyToMessage.From.Username.FirstCharToUpper()} 💬\n";
                    }
                    
                    Console.WriteLine(message.ForwardFromChat?.Username);
                    Console.WriteLine(message.ReplyToMessage?.From.Username);
                    Console.WriteLine(message.ReplyToMessage?.Text);

                    msg = Helpers.GetMessageTop(VkMessageType.Text, sender, message.Text);
                    foreach (var bridge in Program.Bridges)
                    {
                        await VkBot.SendMessageAsync(bridge.VkId, msg);
                    }
                    break;
                case MessageType.Photo:
                    msg = Helpers.GetMessageTop(VkMessageType.Photo, sender, message.Caption);
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
                    msg = Helpers.GetMessageTop(VkMessageType.Video, sender, message.Caption, title: message.Video.FileName);
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
                        await VkBot.SendPhotoAsync(bridge.VkId, string.Empty, filePath);
                    }
                    break;
                case MessageType.Poll:
                    foreach (var bridge in Program.Bridges)
                    {
                        await VkBot.SendMessageAsync(bridge.VkId, Helpers.GetMessageTop(VkMessageType.Poll, sender));
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
