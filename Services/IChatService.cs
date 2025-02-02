using System.Collections.Generic;
using System.Threading.Tasks;

namespace ollez.Services
{
    public interface IChatService
    {
        Task<string> CreateNewSession(string title = "新会话");
        string GetCurrentSessionId();
        void SetCurrentSessionId(string sessionId);
        Task<string> SendMessageAsync(string message, string model);
        Task<IAsyncEnumerable<string>> SendMessageStreamAsync(string message, string model);
    }
}