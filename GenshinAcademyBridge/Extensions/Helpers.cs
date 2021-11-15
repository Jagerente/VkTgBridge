using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinAcademyBridge.Extensions
{
    public static class Helpers
    {
        public static void GetConfig(string cfgPath)
        {
            if (!Directory.Exists(Program.ConfigPath)) Directory.CreateDirectory(Program.ConfigPath);

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

        public static string GetMessageTop(VkMessageType messageType, string sender, string text = "", string reply = "", string title = "")
        {
            switch (messageType)
            {
                case VkMessageType.Text:
                    return $"{sender} 💬\n{text}";
                case VkMessageType.Photo:
                    return $"{sender} 💬\n{text}";
                case VkMessageType.Audio:
                    return $"{sender} 💬\n{text}";
                case VkMessageType.Video:
                    return $"{sender} sent video 🎬\n{title}";
                case VkMessageType.Voice:
                    return $"{sender} sent voice 🎙\n{text}";
                case VkMessageType.Document:
                    return $"{sender} 💬\n{text}";
                case VkMessageType.ChatMembersAdded:
                    return $"{sender} joined.";
                case VkMessageType.ChatMemberLeft:
                    return $"{sender} left.";
                case VkMessageType.Poll:
                    return $"{sender} created a poll 📝\n{text}";
                default:
                    return string.Empty;
            }
        }
    }

    public static class VkExtensions
    {
        public static VkMessageType GetMessageType(this VkNet.Model.Message msg)
        {
            if (msg.Attachments.Count > 0)
            {
                foreach (var attachment in msg.Attachments)
                {
                    var tag = attachment.Instance.ToString();
                    Console.WriteLine(tag);
                    if (tag.Contains("photo"))
                    {
                        return VkMessageType.Photo;
                    }
                    else if (tag.Contains("video"))
                    {
                        return VkMessageType.Video;
                    }
                    else if (tag.Contains("audio_message"))
                    {
                        return VkMessageType.Voice;
                    }
                    else if (tag.Contains("audio"))
                    {
                        return VkMessageType.Audio;
                    }
                    else if (tag.Contains("doc"))
                    {
                        return VkMessageType.Document;
                    }
                    else if (tag.Contains("poll"))
                    {
                        return VkMessageType.Poll;
                    }
                    else if (tag.Contains("sticker"))
                    {
                        return VkMessageType.Sticker;
                    }
                }
            }
            else if (msg.Text != string.Empty)
            {
                return VkMessageType.Text;
            }
            return VkMessageType.Unknown;
        }
    }

    public enum VkMessageType
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
        Poll = 10
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
