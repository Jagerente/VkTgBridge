using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatBridge.Extensions.Telegram
{
    /// <summary>
    /// Configuration for Vk Chat
    /// </summary>
    public class TelegramChatConfiguration
    {
        /// <summary>
        /// Id of Chat
        /// </summary>
        [JsonPropertyName("chatId")]
        public int? ChatId { get; set; }

        /// <summary>
        /// Access token
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; }

        /// <summary>
        /// Types of <seealso cref="BridgeMessageContent"/> to ignore
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("ignoreMessageTypes")]
        public IEnumerable<BridgeMessageContentType> IgnoreMessageTypes { get; set; }
    }
}
