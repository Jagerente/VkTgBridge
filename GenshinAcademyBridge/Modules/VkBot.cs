using GenshinAcademyBridge.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VkNet;
using VkNet.Extensions.Polling;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Model;
using VkNet.Model.GroupUpdate;

namespace GenshinAcademyBridge.Modules
{
    class VkBot
    {
        public static VkApi VkApi { get; private set; }

        public const string VkConfigPath = Program.ConfigPath + "vkConfig.json";

        public static Configuration.VkConfiguration VkConfig;

        public VkBot()
        {
            SetupVK();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            GroupLongPoll groupLongPoll = VkApi.StartGroupLongPollAsync(GroupLongPollConfiguration.Default, cancellationTokenSource.Token);

            StartReceiving(groupLongPoll.AsChannelReader(), GetUpdates);
        }

        private static async Task StartReceiving<TUpdate>(ChannelReader<TUpdate> channelReader, Action<TUpdate> updateAction)
        {
            IAsyncEnumerable<TUpdate> updateAsyncEnumerable = channelReader.ReadAllAsync();

            await foreach (TUpdate update in updateAsyncEnumerable)
            {
                updateAction(update);
            }
        }

        private static void SetupVK()
        {
            Helpers.GetConfig(VkConfigPath);

            VkConfig = JsonStorage.RestoreObject<Configuration.VkConfiguration>(VkConfigPath);
            VkApi = new VkApi(Program.Services);

            VkApi.Authorize(new ApiAuthParams
            {
                AccessToken = VkConfig.Token
            });

            VkApi.VkApiVersion.SetVersion(5, 131);

            Serilog.Log.Information($"VK Bot has started!");
        }

        public static async Task<long> SendMessageAsync(long conversationId, string message)
        {
            long msgId = 0;

                msgId = await VkApi.Messages.SendAsync(new VkNet.Model.RequestParams.MessagesSendParams { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = message });
            return msgId;
        }

        internal static async Task<long> SendPhotoAsync(long conversationId, string message, string file )
        {
            var uploadServer = await VkApi.Photo.GetMessagesUploadServerAsync((long)VkConfig.GroupId);
            var response = UploadFile(uploadServer.UploadUrl, file);
            var attachment = await VkApi.Photo.SaveMessagesPhotoAsync(response);
            return VkApi.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = message, Attachments = attachment });
        }

        internal static async Task<long> SendStickerAsync(long conversationId, string title, string file)
        {
            var uploadServer = await VkApi.Docs.GetUploadServerAsync((long)VkConfig.GroupId);
            var response = UploadFile(uploadServer.UploadUrl, file);
            var attachment = new[] { VkApi.Docs.Save(response, title, null).FirstOrDefault().Instance };
            return VkApi.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = string.Empty, Attachments = attachment });
        }

        internal static async Task<long> SendPollAsync(long conversationId, string question, string[] options, bool? isAnonymous, bool? allowsMultipleAnswers)
        {
            var poll = await VkApi.PollsCategory.CreateAsync(new VkNet.Model.RequestParams.Polls.PollsCreateParams { Question = question, AddAnswers = options, IsAnonymous = isAnonymous, IsMultiple = allowsMultipleAnswers });
            return VkApi.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = string.Empty, Attachments = new[] { poll } });
        }


        private static string UploadFile(string serverUrl, string file)
        {
            var wc = new WebClient();
            return Encoding.ASCII.GetString(wc.UploadFile(serverUrl, file));
        }

        private static void GetUpdates(GroupUpdate groupUpdate)
        {
            if (Program.Bridges.Any(x => 2000000000 + x.VkId == groupUpdate.MessageNew.Message.PeerId))
            {
                var user = VkApi.Users.Get(new List<long>() { groupUpdate.MessageNew.Message.FromId.GetValueOrDefault() })[0];

                Serilog.Log.ForContext("Update", groupUpdate).Information("Got a message.");
                var sender = $"{user.FirstName} {user.LastName}";
                string[] urls = new string[] { };

                string message = groupUpdate.MessageNew.Message.Text;
                switch (groupUpdate.MessageNew.Message.GetMessageType())
                {
                    case VkMessageType.Text:
                        foreach (var bridge in Program.Bridges)
                        {
                            TgBot.SendMessageAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Text, sender, message));
                        }
                        break;
                    case VkMessageType.Photo:
                        foreach (var attachment in groupUpdate.MessageNew.Message.Attachments)
                        {
                            var tag = attachment.Instance.ToString();
                            if (tag.Contains("photo"))
                            {
                                urls = urls.Append($"{(attachment.Instance as VkNet.Model.Attachments.Photo).Sizes.Last().Url.AbsoluteUri}").ToArray();
                            }
                        }
                        foreach (var bridge in Program.Bridges)
                        {
                            TgBot.SendPhotoAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Photo, sender, message), urls);
                        }
                        break;
                    case VkMessageType.Video:
                        if (message != string.Empty)
                        {
                            foreach (var bridge in Program.Bridges)
                            {
                                TgBot.SendMessageAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Text, sender, message));
                            }
                        }
                        foreach (var attachment in groupUpdate.MessageNew.Message.Attachments)
                        {
                            var tag = attachment.Instance.ToString();
                            var titles = new string[] { };
                            var sources = new string[] { };
                            urls = new string[] { };
                            if (tag.Contains("video"))
                            {
                                var video = attachment.Instance as VkNet.Model.Attachments.Video;
                                titles = titles.Append($"«{video.Title}»").ToArray();
                                urls = urls.Append($"{video.Image.Last().Url.AbsoluteUri}").ToArray();
                                foreach (var bridge in Program.Bridges)
                                {
                                    TgBot.SendVideoAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Video, sender, title: string.Join("\n", titles)), urls);
                                }
                            }
                        }
                        break;
                    case VkMessageType.Sticker:
                        var sticker = (groupUpdate.MessageNew.Message.Attachments.FirstOrDefault().Instance as VkNet.Model.Attachments.Sticker).Images.LastOrDefault().Url.ToString();
                        foreach (var bridge in Program.Bridges)
                        {
                            TgBot.SendStickerAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Sticker, sender), sticker);
                        }
                        break;
                    case VkMessageType.Poll:
                        if (message != string.Empty)
                        {
                            foreach (var bridge in Program.Bridges)
                            {
                                TgBot.SendMessageAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Text, sender, message));
                            }
                        }

                        var poll = (groupUpdate.MessageNew.Message.Attachments.FirstOrDefault().Instance as VkNet.Model.Attachments.Poll);
                        foreach (var bridge in Program.Bridges)
                        {
                            TgBot.SendPollAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Poll, sender, poll.Question), poll.Answers.Select(x => x.Text).ToArray(), poll.Anonymous, poll.Multiple);
                        }
                        break;
                }
            }

        }

    }
}
