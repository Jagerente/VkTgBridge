using System;
using System.Threading.Tasks;

namespace GenshinAcademyBridge
{
    public interface IChat
    {
        Task InitializeAsync();
        Task<IObservable<TextMessage>> StartListenAsync();
    }
}
