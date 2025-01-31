using System.Threading.Tasks;

namespace ollez.Services
{
    public interface IChatService
    {
        Task<string> SendMessageAsync(string message, string model);
    }
} 