using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
