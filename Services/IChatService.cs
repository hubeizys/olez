using System.Collections.Generic;
using System.Threading.Tasks;

namespace ollez.Services
{
    public interface IChatService
    {
        Task<string> SendMessageAsync(string message, string model);
        Task<IAsyncEnumerable<string>> SendMessageStreamAsync(string message, string model);
    }
} 