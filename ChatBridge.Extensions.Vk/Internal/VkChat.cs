using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ChatBridge.MessageContent;
using VkNet;
using VkNet.Model;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Extensions.Polling;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Extensions.Polling.Models.Update;
using VkNet.Model.GroupUpdate;
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
            if (message.Type == MessageType.Sended || string.IsNullOrWhiteSpace(message.Text))
            {
                return null;
            }
            long fromId = message.FromId.GetValueOrDefault();
            User sender = _chatUsers.FirstOrDefault(x => x.Id == fromId);

            string senderName = sender == null ? "Unknown" : $"{sender.FirstName} {sender.LastName}";

            return new BridgeMessage(this, senderName, new BridgeMessageContent[] { new CommonTextContent(message.Text) });
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
            await _api.AuthorizeAsync(new ApiAuthParams()
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
            await _api.Messages.SendAsync(new MessagesSendParams()
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
