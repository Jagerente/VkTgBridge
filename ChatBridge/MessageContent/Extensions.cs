using System;

namespace ChatBridge.MessageContent
{
    public static class Extensions
    {
        public static TextContent AsTextContent(this BridgeMessageContent content)
        {
            if (content.Type == BridgeMessageContentType.Text && content is TextContent resultContent)
            {
                return resultContent;
            }
            throw new InvalidOperationException();
        }

        public static PhotoContent AsPhotoContent(this BridgeMessageContent content)
        {
            if (content.Type == BridgeMessageContentType.Photo && content is PhotoContent resultContent)
            {
                return resultContent;
            }
            throw new InvalidOperationException();
        }

        public static VideoContent AsVideoContent(this BridgeMessageContent content)
        {
            if (content.Type == BridgeMessageContentType.Video && content is VideoContent resultContent)
            {
                return resultContent;
            }
            throw new InvalidOperationException();
        }

        public static StickerContent AsStickerContent(this BridgeMessageContent content)
        {
            if (content.Type == BridgeMessageContentType.Sticker && content is StickerContent resultContent)
            {
                return resultContent;
            }
            throw new InvalidOperationException();
        }

        public static PollContent AsPollContent(this BridgeMessageContent content)
        {
            if (content.Type == BridgeMessageContentType.Poll && content is PollContent resultContent)
            {
                return resultContent;
            }
            throw new InvalidOperationException();
        }

        public static string FormMessage(this BridgeMessageContent content)
        {
            switch (content.Type)
            {
                case BridgeMessageContentType.Text:
                    return $"{content.AsTextContent().Sender} 💬\n{content.AsTextContent().Text}";
                //case BridgeMessageContentType.Reply:
                //    return $"Reply to {reply} 💬\n{content.AsTextContent().Text}";
                //case BridgeMessageContentType.Forwarded:
                //    return $"{sender} forward from {reply} 💬\n{text}";
                case BridgeMessageContentType.Photo:
                    return $"{content.AsPhotoContent().Sender} 💬\n{content.AsPhotoContent().Caption}";
                //case BridgeMessageContentType.Audio:
                //    return $"{sender} 💬\n{text}";
                case BridgeMessageContentType.Video:
                    return $"{content.AsVideoContent().Sender} sent video 🎬\n{content.AsVideoContent().Title}";
                //case BridgeMessageContentType.Voice:
                //    return $"{content} sent voice 🎙\n{text}";
                //case BridgeMessageContentType.Document:
                //    return $"{sender} 💬\n{text}";
                //case BridgeMessageContentType.ChatMembersAdded:
                //    return $"{sender} joined.";
                //case BridgeMessageContentType.ChatMemberLeft:
                //    return $"{sender} left.";
                case BridgeMessageContentType.Poll:
                    return $"{content.AsPollContent().Sender} created a poll 📝\n{content.AsPollContent().Caption}";
                case BridgeMessageContentType.Sticker:
                    return $"{content.AsStickerContent().Sender} 💬";
                default:
                    return string.Empty;
            }
        }

        public static string FirstCharToUpper(this string input)
            => !string.IsNullOrEmpty(input) ? string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1)) : null;
    }
}
