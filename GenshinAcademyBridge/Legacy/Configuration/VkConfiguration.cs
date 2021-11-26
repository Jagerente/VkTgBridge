using Newtonsoft.Json;

namespace GenshinAcademyBridge.Configuration
{
    public class VkConfiguration : ApiConfiguration
    {
        [JsonProperty("groupId")]
        public ulong GroupId { get; set; }
    }
}
