using Newtonsoft.Json;

namespace GenshinAcademyBridge.Configuration
{
    public class ApiConfiguration
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
