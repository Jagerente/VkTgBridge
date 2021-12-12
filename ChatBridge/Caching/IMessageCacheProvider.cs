using System;

namespace ChatBridge.Caching
{
    public interface IMessageCacheProvider
    {
        IMessageCache GetCacheForService(string serviceName);
    }
}
