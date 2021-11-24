using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBridge
{
    /// <summary>
    /// Base interface for Chat
    /// </summary>
    public interface IBridgeChat
    {
        /// <summary>
        /// Name of service this Chat represents (Like Discord, Telegram, etc...)
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Initializes Chat and ensures everything is ready to be runned
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task InitializeAsync(CancellationToken cancelToken = default);

        /// <summary>
        /// Sends message to the Chat
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task SendMessageAsync(BridgeMessage message, CancellationToken cancelToken = default);

        /// <summary>
        /// Starts listening messages in Chat
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns>Observable to listen incoming messages from the Chat</returns>
        Task<IObservable<BridgeMessage>> StartListenAsync(CancellationToken cancelToken = default);
    }
}
