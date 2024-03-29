﻿using GenshinAcademyBridge.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Extensions.Polling;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Extensions.Polling.Models.Update;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;

namespace GenshinAcademyBridge.Modules
{
    class VkBot
    {
        public static VkApi VkApi { get; private set; }

        public const string VkConfigPath = Program.ConfigPath + "vkConfig.json";

        public static Configuration.VkConfiguration VkConfig;

        public VkBot()
        {
            SetupVk();
            var cancellationTokenSource = new CancellationTokenSource();
            if (VkApi.IsAuthorizedAsUser())
            {
                UserLongPoll userLongPoll = VkApi.StartUserLongPollAsync(UserLongPollConfiguration.Default, cancellationTokenSource.Token);

                StartReceiving(userLongPoll.AsChannelReader(), GetUserUpdates);
            }
            else if (VkApi.IsAuthorizedAsGroup())
            {
                GroupLongPoll groupLongPoll = VkApi.StartGroupLongPollAsync(GroupLongPollConfiguration.Default,
                    cancellationTokenSource.Token);

                StartReceiving(groupLongPoll.AsChannelReader(), GetGroupUpdates);
            }
        }

        private static async Task StartReceiving<TUpdate>(ChannelReader<TUpdate> channelReader, Action<TUpdate> updateAction)
        {
            IAsyncEnumerable<TUpdate> updateAsyncEnumerable = channelReader.ReadAllAsync();

            await foreach (TUpdate update in updateAsyncEnumerable)
            {
                updateAction(update);
            }
        }

        private static void SetupVk()
        {
            Helpers.GetConfig(VkConfigPath);

            VkConfig = JsonStorage.RestoreObject<Configuration.VkConfiguration>(VkConfigPath);
            VkApi = new VkApi(Program.Services);

            VkApi.Authorize(new ApiAuthParams
            {
                AccessToken = VkConfig.Token
            });

            VkApi.VkApiVersion.SetVersion(5, 131);

            Serilog.Log.Information($"VK Bot {VkApi} has started!");
        }

        public static async Task<long> SendMessageAsync(long conversationId, string message)
        {
            var msgId = await VkApi.Messages.SendAsync(new VkNet.Model.RequestParams.MessagesSendParams
                {PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = message});
            return msgId;
        }

        internal static async Task<long> SendPhotoAsync(long conversationId, string message, string file )
        {
            var uploadServer = await VkApi.Photo.GetMessagesUploadServerAsync(0);
            var response = UploadFile(uploadServer.UploadUrl, file);
            var attachment = await VkApi.Photo.SaveMessagesPhotoAsync(response);
            return VkApi.Messages.Send(new VkNet.Model.RequestParams.MessagesSendParams { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = message, Attachments = attachment });
            
        }

        internal static async Task<long> SendStickerAsync(long conversationId, string title, string file)
        {
            var uploadServer = await VkApi.Docs.GetMessagesUploadServerAsync(0, DocMessageType.Graffiti);
            var response = UploadFile(uploadServer.UploadUrl, file);
            var attachment = new List<MediaAttachment> { VkApi.Docs.SaveAsync(response, new Random().Next(int.MaxValue).ToString(), null).Result.FirstOrDefault().Instance};
            return VkApi.Messages.Send(new MessagesSendParams { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = title, Attachments = attachment });
        }

        internal static async Task<long> SendPollAsync(long conversationId, string question, string[] options, bool? isAnonymous, bool? allowsMultipleAnswers)
        {
            var poll = await VkApi.PollsCategory.CreateAsync(new VkNet.Model.RequestParams.Polls.PollsCreateParams { Question = question, AddAnswers = options, IsAnonymous = isAnonymous, IsMultiple = allowsMultipleAnswers });
            return VkApi.Messages.Send(new MessagesSendParams { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = string.Empty, Attachments = new[] { poll } });
        }

        internal static async Task<long> ReplyAsync(long conversationId, string message, long id)
        {
            var msgId = await VkApi.Messages.SendAsync(new VkNet.Model.RequestParams.MessagesSendParams
                { PeerId = 2000000000 + conversationId, RandomId = new Random().Next(int.MaxValue), Message = message, ReplyTo = id});
            return msgId;
        }


        private static string UploadFile(string serverUrl, string file)
        {
            var wc = new WebClient();
            return Encoding.ASCII.GetString(wc.UploadFile(serverUrl, file));
        }

        private static void GetGroupUpdates(GroupUpdate groupUpdate)
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
                                urls = urls.Append($"{((Photo) attachment.Instance).Sizes.Last().Url.AbsoluteUri}").ToArray();
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
                            urls = new string[] { };
                            if (tag.Contains("video"))
                            {
                                var video = (Video) attachment.Instance;
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
                        var sticker = ((Sticker) groupUpdate.MessageNew.Message.Attachments.FirstOrDefault().Instance).Images.LastOrDefault().Url.ToString();
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

                        var poll = (Poll) groupUpdate.MessageNew.Message.Attachments.FirstOrDefault().Instance;
                        foreach (var bridge in Program.Bridges)
                        {
                            TgBot.SendPollAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Poll, sender, poll.Question), poll.Answers.Select(x => x.Text).ToArray(), poll.Anonymous, poll.Multiple);
                        }
                        break;
                }
            }

        }

        private static void GetUserUpdates(UserUpdate userUpdate)
        {
            if (userUpdate.Sender.User.Id ==  VkApi.Users.Get(new List<long>()).FirstOrDefault().Id)
                return;

            if (Program.Bridges.Any(x => 2000000000 + x.VkId == userUpdate.Message.PeerId))
            {
                var user = VkApi.Users.Get(new List<long>() { userUpdate.Message.FromId.GetValueOrDefault() })[0];

                Serilog.Log.ForContext("Update", userUpdate).Information("Got a message.");
                var sender = $"{user.FirstName} {user.LastName}";
                string[] urls = new string[] { };

                string message = userUpdate.Message.Text;
                switch (userUpdate.Message.GetMessageType())
                {
                    case VkMessageType.Text:
                        if (userUpdate.Message.ReplyMessage != null)
                        {
                            foreach (var bridge in Program.Bridges)
                            {
                                Console.WriteLine((long)userUpdate.Message.ReplyMessage.ConversationMessageId);
                                Program.MessagesIds.Add((long)userUpdate.Message.ConversationMessageId, TgBot.ReplyAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Text, sender, message), Program.MessagesIds[(long)userUpdate.Message.ReplyMessage.ConversationMessageId]).Result);
                            }
                            break;
                        }

                        foreach (var bridge in Program.Bridges)
                        {
                            Program.MessagesIds.Add((long)userUpdate.Message.ConversationMessageId, TgBot.SendMessageAsync(bridge.TgId, Helpers.GetMessageTop(VkMessageType.Text, sender, message)).Result);
                        }
                        break;
                    case VkMessageType.Photo:
                        foreach (var attachment in userUpdate.Message.Attachments)
                        {
                            var tag = attachment.Instance.ToString();
                            if (tag.Contains("photo"))
                            {
                                urls = urls.Append($"{((Photo)attachment.Instance).Sizes.Last().Url.AbsoluteUri}").ToArray();
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
                        foreach (var attachment in userUpdate.Message.Attachments)
                        {
                            var tag = attachment.Instance.ToString();
                            var titles = new string[] { };
                            urls = new string[] { };
                            if (tag.Contains("video"))
                            {
                                var video = (Video)attachment.Instance;
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
                        var sticker = ((Sticker)userUpdate.Message.Attachments.FirstOrDefault().Instance).Images.LastOrDefault().Url.ToString();
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

                        var poll = (Poll)userUpdate.Message.Attachments.FirstOrDefault().Instance;
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
