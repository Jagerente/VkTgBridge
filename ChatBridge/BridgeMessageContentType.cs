namespace ChatBridge
{
    //TODO:?
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
        //Maybe separate resources by url and by data/bytes?
        //PhotoLink,
        //VideoLink,
    }
}
