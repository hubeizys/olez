using System.Collections.Generic;
using System.Threading.Tasks;
using ollez.Data.Models;

namespace ollez.Services
{
    public interface IChatDbService
    {
        Task<DbChatSession> CreateSessionAsync(string title);
        Task<DbChatSession?> GetSessionAsync(string id);
        Task<List<DbChatSession>> GetAllSessionsAsync();
        Task<DbChatMessage> AddMessageAsync(string sessionId, string content, bool isUser);
        Task DeleteSessionAsync(string id);
        Task<int> SaveChangesAsync();
    }
}