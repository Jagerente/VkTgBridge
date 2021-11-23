using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace GenshinAcademyBridge.Extensions
{
    public static class Helpers
    {
        public static void GetConfig(string cfgPath)
        {
            if (!Directory.Exists(ChatBridgeService.ConfigPath)) Directory.CreateDirectory(ChatBridgeService.ConfigPath);

            if (!File.Exists(cfgPath))
            {
                var cfg = new Configuration.VkConfiguration();
                JsonStorage.StoreObject(cfg, cfgPath);
                foreach (var property in JObject.Parse(JsonConvert.SerializeObject(cfg)))
                {
                    Console.WriteLine($"Set {cfgPath.Split("/").LastOrDefault()} {property.Key}:");
                    JsonStorage.SetValue(cfgPath, property.Key, Console.ReadLine());
                }
            }
        }

        public static string GetMessageTop(BridgeMessageType messageType, string sender, string text = "", string reply = "", string title = "")
        {
            switch (messageType)
            {
                case BridgeMessageType.Text:
                    return $"{sender} 💬\n{text}";
                case BridgeMessageType.Reply:
                    return $"Reply to {reply} 💬\n{text}";
                case BridgeMessageType.Forwarded:
                    return $"{sender} forward from {reply} 💬\n{text}";
                case BridgeMessageType.Photo:
                    return $"{sender} 💬\n{text}";
                case BridgeMessageType.Audio:
                    return $"{sender} 💬\n{text}";
                case BridgeMessageType.Video:
                    return $"{sender} sent video 🎬\n{title}";
                case BridgeMessageType.Voice:
                    return $"{sender} sent voice 🎙\n{text}";
                case BridgeMessageType.Document:
                    return $"{sender} 💬\n{text}";
                case BridgeMessageType.ChatMembersAdded:
                    return $"{sender} joined.";
                case BridgeMessageType.ChatMemberLeft:
                    return $"{sender} left.";
                case BridgeMessageType.Poll:
                    return $"{sender} created a poll 📝\n{text}";
                case BridgeMessageType.Sticker:
                    return $"{sender} 💬";
                default:
                    return string.Empty;
            }
        }
    }

    public static class VkExtensions
    {
        public static BridgeMessageType GetMessageType(this VkNet.Model.Message msg)
        {
            if (msg.Attachments.Count > 0)
            {
                foreach (var attachment in msg.Attachments)
                {
                    var tag = attachment.Instance.ToString();
                    Console.WriteLine(tag);
                    if (tag.Contains("photo"))
                    {
                        return BridgeMessageType.Photo;
                    }
                    else if (tag.Contains("video"))
                    {
                        return BridgeMessageType.Video;
                    }
                    else if (tag.Contains("audio_message"))
                    {
                        return BridgeMessageType.Voice;
                    }
                    else if (tag.Contains("audio"))
                    {
                        return BridgeMessageType.Audio;
                    }
                    else if (tag.Contains("doc"))
                    {
                        return BridgeMessageType.Document;
                    }
                    else if (tag.Contains("poll"))
                    {
                        return BridgeMessageType.Poll;
                    }
                    else if (tag.Contains("sticker"))
                    {
                        return BridgeMessageType.Sticker;
                    }
                }
            }
            else if (msg.Text != string.Empty)
            {
                return BridgeMessageType.Text;
            }
            return BridgeMessageType.Unknown;
        }
    }

    public enum BridgeMessageType
    {
        Unknown = 0,
        Text = 1,
        Photo = 2,
        Audio = 3,
        Video = 4,
        Voice = 5,
        Document = 6,
        Sticker = 7,
        ChatMembersAdded = 8,
        ChatMemberLeft = 9,
        Poll = 10,
        Reply = 11,
        Forwarded = 12
    }


    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };
    }
}
