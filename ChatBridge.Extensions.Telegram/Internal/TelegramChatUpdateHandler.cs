using ChatBridge.MessageContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ChatBridge.Extensions.Telegram.Internal
{
    internal class TelegramChatUpdateHandler : IUpdateHandler
    {
        private readonly TelegramChat _chat;
        private readonly Subject<BridgeMessage> _messageSource;
        private readonly IReadOnlyCollection<BridgeMessageContentType> _contentTypes;

        public TelegramChatUpdateHandler(
            TelegramChat chat,
            Subject<BridgeMessage> subject,
            IEnumerable<BridgeMessageContentType> allowedTypes)
        {
            _chat = chat;
            _messageSource = subject;
            _contentTypes = allowedTypes == null ? new List<BridgeMessageContentType>() : new List<BridgeMessageContentType>(allowedTypes);
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _messageSource.OnError(exception);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            string senderName = update.Message.From.Username ?? $"{update.Message.From.FirstName} {update.Message.From.LastName}";
            BridgeMessage receivedMessage = null;
            if(update.Message.Type == MessageType.Text)
            {
                receivedMessage = new BridgeMessage(_chat, senderName, new BridgeMessageContent[]
                {
                    new CommonTextContent(update.Message.Text)
                });
            }

            if(receivedMessage != null)
            {
                _messageSource.OnNext(receivedMessage);
            }
        }
    }
}
