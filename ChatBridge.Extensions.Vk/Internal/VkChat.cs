using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChatBridge.MessageContent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.Enums;
using VkNet.Extensions.Polling;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Extensions.Polling.Models.Update;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace ChatBridge.Extensions.Vk.Internal
{
    /// <summary>
    /// Chat for Vk implementing <seealso cref="IBridgeChat"/> interface
    /// </summary>
    internal class VkChat : IBridgeChat
    {
        private readonly Random _random;
        private readonly VkApi _api;
        private readonly VkChatConfiguration _configuration;
        private readonly ILogger<VkChat> _logger;
        private Subject<BridgeMessage> _mesageSubject;
        private UserLongPoll _longPoll;
        private readonly long _peerId;
        private List<User> _chatUsers; 

        public string ServiceName => "Vk";

        public VkChat(
            Random random,
            IServiceCollection services,
            ILogger<VkChat> logger,
            VkChatConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _api = new VkApi(services);
            _peerId = Constants.GroupChatIdPrefix + configuration.ChatId.Value;
            _random = random;
        }

        /// <summary>
        /// Reads message from update
        /// </summary>
        /// <param name="update">Update event</param>
        /// <returns>null if event type is not supported or be parsed; <seealso cref="BridgeMessage"/> to send to other Chats otherwise</returns>
        private async Task<BridgeMessage> ReadMessageFromUpdateAsync(UserUpdate update)
        {
            await Task.CompletedTask;
            if (update.Message.PeerId.Value != _peerId)
            {
                return null;
            }
            Message message = update.Message;


            if (message.Type == MessageType.Sended)
            {
                return null;
            }
            long fromId = message.FromId.GetValueOrDefault();
            User sender = _chatUsers.FirstOrDefault(x => x.Id == fromId);

            List<BridgeMessageContent> contents = new List<BridgeMessageContent>();

            foreach (var attachment in message.Attachments)
            {
                var tag = attachment.Instance.ToString();
                Console.WriteLine(tag);
                var type = BridgeMessageContentType.Text;
                Console.WriteLine(tag);
                if (tag.Contains("sticker"))
                {
                    Console.WriteLine("vk:sticker");

                    var sticker = ((Sticker)attachment.Instance).Images.First().Url.ToString();
                    Console.WriteLine(((Sticker)attachment.Instance).Images.Count());
                    contents.Add(new StickerContent(sticker));
                }
                else if (tag.Contains("photo"))
                {
                    Console.WriteLine("vk:photo");

                    contents.Add(new PhotoContent(((Photo)attachment.Instance).Sizes.Last().Url.AbsoluteUri, message.Attachments.Count > 1 ? string.Empty : message.Text));
                }
                else if (tag.Contains("video"))
                {
                    Console.WriteLine("vk:video");

                    var video = (Video)attachment.Instance;
                    var title = video.Title;
                    var url = $"{video.Image.Last().Url.AbsoluteUri}";
                    contents.Add(new VideoContent(url, title, message.Attachments.Count > 1 ? string.Empty : message.Text));
                }

                else if (tag.Contains("audio_message"))
                {
                    Console.WriteLine(tag + "sent");
                }
                else if (tag.Contains("audio"))
                {
                    Console.WriteLine(tag + "sent");
                }
                else if (tag.Contains("doc"))
                {
                    Console.WriteLine(tag + "sent");
                }
                else if (tag.Contains("poll"))
                {
                    Console.WriteLine("vk:poll");

                    var poll = (Poll)attachment.Instance;
                    var question = poll.Question;
                    var options = poll.Answers.Select(x => x.Text).ToArray();
                    var isAnonymous = poll.Anonymous;
                    var isMultiple = poll.Multiple;

                    contents.Add(new PollContent(question, options, isAnonymous, isMultiple));
                }
            }

            if (message.Attachments.Count != 1)
            {
                Console.WriteLine("vk:text");
                contents.Add(new TextContent(message.Text));
            }

            string senderName = sender == null ? "Unknown" : $"{sender.FirstName} {sender.LastName}";

            return new BridgeMessage(this, senderName, contents);
        }

        /// <summary>
        /// Runs listening updates of the client
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        private async Task StartListeningUpdates(CancellationToken cancelToken)
        {
            ChannelReader<UserUpdate> channel = _longPoll.AsChannelReader();

            while (cancelToken.IsCancellationRequested == false)
            {
                bool canContinue = await channel.WaitToReadAsync(cancelToken);
                if (!canContinue)
                {
                    break;
                }
                UserUpdate update = await channel.ReadAsync(cancelToken);
                BridgeMessage message = await ReadMessageFromUpdateAsync(update);
                if (message == null)
                {
                    continue;
                }
                _mesageSubject.OnNext(message);
            }
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancelToken = default)
        {
            _logger.LogInformation("Start initializing Vk Chat");
            await _api.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = _configuration.Token
            });
            _api.VkApiVersion.SetVersion(5, 131);

            //Getting and caching all users of conversation, to not send get request every time we need their info (name, surname)
            GetConversationMembersResult chatMembers = await _api.Messages.GetConversationMembersAsync(_peerId);
            _chatUsers = new List<User>(chatMembers.Profiles);

            _logger.LogInformation("Initializing Vk Chat complete successfully");
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(BridgeMessage message, CancellationToken cancelToken = default)
        {
            var content = (string)await message.FirstOrDefault().GetDataAsync();
            await _api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = _peerId,
                Message = content,
                RandomId = _random.Next(int.MaxValue)
            });
        }

        /// <inheritdoc/>
        public async Task<IObservable<BridgeMessage>> StartListenAsync(CancellationToken cancelToken = default)
        {
            _logger.LogInformation("Running Vk Chat...");

            _mesageSubject = new Subject<BridgeMessage>();
            _longPoll = _api.StartUserLongPollAsync(UserLongPollConfiguration.Default, cancelToken);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            StartListeningUpdates(cancelToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.CompletedTask;

            _logger.LogInformation("Vk Chat is running.");
            return _mesageSubject;
        }
    }
}
