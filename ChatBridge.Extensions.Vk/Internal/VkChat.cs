using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model;
using VkNet.Extensions.Polling;
using VkNet.Extensions.Polling.Models.Configuration;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Channels;
using VkNet.Model.GroupUpdate;
using VkNet.Enums.SafetyEnums;
using VkNet.Enums;
using VkNet.Enums.Filters;
using ChatBridge.MessageContent;
using VkNet.Extensions.Polling.Models.Update;

namespace ChatBridge.Extensions.Vk.Internal
{
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
        //private

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
            _peerId = 2000000000 + configuration.ChatId.Value;
            _random = random;
        }

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

        public async Task InitializeAsync(CancellationToken cancelToken = default)
        {
            _logger.LogInformation("Start initializing");
            await _api.AuthorizeAsync(new ApiAuthParams()
            {
                AccessToken = _configuration.Token
            });
            _api.VkApiVersion.SetVersion(5, 131);

            GetConversationMembersResult chatMembers = await _api.Messages.GetConversationMembersAsync(_peerId);
            _chatUsers = new List<User>(chatMembers.Profiles);

            _logger.LogInformation("Initializing complete successfully");
        }

        public Task SendMessageAsync(BridgeMessage message, CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<IObservable<BridgeMessage>> StartListenAsync(CancellationToken cancelToken = default)
        {
            _mesageSubject = new Subject<BridgeMessage>();
            _longPoll = _api.StartUserLongPollAsync(UserLongPollConfiguration.Default, cancelToken);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            StartListeningUpdates(cancelToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.CompletedTask;
            return _mesageSubject;
        }
    }
}
