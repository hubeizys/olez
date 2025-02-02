using System.Collections.Generic;
using System.Threading.Tasks;
using ollez.Models;

namespace ollez.Services
{
    public interface IChatDbService
    {
        Task<ChatSession> CreateSessionAsync(string title);
        Task<ChatSession?> GetSessionAsync(string id);
        Task<List<ChatSession>> GetAllSessionsAsync();
        Task<ChatMessage> AddMessageAsync(string sessionId, string content, bool isUser);
        Task DeleteSessionAsync(string id);
        Task<int> SaveChangesAsync();
    }
}