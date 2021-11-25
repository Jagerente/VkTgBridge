using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge
{
    public enum BridgeMessageContentType
    {
        Unknown,
        Text,
        Photo,
        Audio,
        Video,
        Voice,
        Document,
        Sticker,
        ChatMembersAdded,
        ChatMemberLeft,
        Poll,
        Reply,
        Forwarded
    }
}
