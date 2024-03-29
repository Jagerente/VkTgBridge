﻿using System;
using GenshinAcademyBridge.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace GenshinAcademyBridge.Modules
{
    class TgBot
    {
        private const string TgConfigPath = Program.ConfigPath + "tgConfig.json";

        public static TelegramBotClient TgApi { get; private set; }

        public static Configuration.TgConfiguration TgConfig { get; private set; }


        public TgBot()
        {
            SetupTgAsync();
        }

        private static async void SetupTgAsync()
        {
            Helpers.GetConfig(TgConfigPath);

            TgConfig = JsonStorage.RestoreObject<Configuration.TgConfiguration>(TgConfigPath);

            TgApi = new TelegramBotClient(TgConfig.Token);
            var me = await TgApi.GetMeAsync();
            Serilog.Log.Information($"Telegram Bot has started! {me.Username}:{me.Id}");

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { Telegram.Bot.Types.Enums.UpdateType.Message }, // Receiving messages only
                ThrowPendingUpdates = true
            };
            TgApi.StartReceiving(
                TgHandlers.HandleUpdateAsync,
                TgHandlers.HandleErrorAsync,
                receiverOptions
            );
        }


        public static async Task<long> SendMessageAsync(long conversationId, string message)
        {
            Serilog.Log.ForContext("Message", message).Information($"Sent a message to TG.");
            return Convert.ToInt64((await TgApi.SendTextMessageAsync(chatId: conversationId, text: message)).MessageId);
        }

        public static async Task<long> ReplyAsync(long conversationId, string message, long id)
        {
            Serilog.Log.ForContext("Message", message).Information($"Sent a message to TG.");
            return Convert.ToInt64((await TgApi.SendTextMessageAsync(chatId: conversationId, text: message, replyToMessageId: Convert.ToInt32(id))).MessageId);
        }

        public static async Task SendStickerAsync(long conversationId, string message, string url)
        {
            await SendPhotoAsync(conversationId, message, url);
        }

        public static async Task SendPhotoAsync(long conversationId, string message, string url)
        {
            await TgApi.SendPhotoAsync(chatId: conversationId, photo: url, caption: message);
            Serilog.Log.ForContext("Photos", url).Information($"Sent {url.Length} photos to TG.");
        }

        public static async Task SendPhotoAsync(long conversationId, string message, string[] url)
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
                Serilog.Log.ForContext("Photos", url).Information($"Sent a photo to TG.");
                await TgApi.SendMediaGroupAsync(chatId: conversationId, media: photos);
                await SendMessageAsync(conversationId, message);
            }
        }

        public static async Task SendVideoAsync(long conversationId, string message, string[] urls)
        {
            foreach (var url in urls)
            {
                await TgApi.SendPhotoAsync(chatId: conversationId, photo: url, caption: message);
            }
            Serilog.Log.Information($"Sent {urls.Length} videos to TG.");


            //if (url.Length == 1)
            //{
            //        await TgApi.SendPhotoAsync(chatId: bridge.TgId, photo: url.FirstOrDefault(), caption: message);
            //        Serilog.Log.Information($"Sent a video to TG.");
            //}
            //else
            //{
            //    var photos = new IAlbumInputMedia[] { };
            //    foreach (var photo in url)
            //    {
            //        photos = photos.Append(new InputMediaPhoto(photo)).ToArray();
            //    }
            //        await SendMessageAsync(message);
            //        await TgApi.SendMediaGroupAsync(chatId: bridge.TgId, media: photos);

            //    Serilog.Log.Information($"Sent {url.Length} videos to TG.");
            //}
        }

        public static async Task SendPollAsync(long conversationId, string question, string[] options, bool? isAnonymous, bool? allowsMultipleAnswers)
        {
            await TgApi.SendPollAsync(conversationId, $"{question}", options, isAnonymous, allowsMultipleAnswers: allowsMultipleAnswers);

            Serilog.Log.Information($"Sent poll to TG.");
        }
    }
}