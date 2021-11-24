using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatBridge.Extensions.Vk
{
    public class VkChatConfiguration
    {
        /// <summary>
        /// Id of Chat on the account
        /// </summary>
        [JsonPropertyName("chatId")]
        public int? ChatId { get; set; }

        /// <summary>
        /// Id of the group
        /// </summary>
        [JsonPropertyName("groupId")]
        public long? GroupId { get; set; }

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
