using Microsoft.EntityFrameworkCore;
using ollez.Data;
using ollez.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ollez.Services
{
    public class ChatDbService : IChatDbService
    {
        private readonly Func<ChatDbContext> _contextFactory;

        public ChatDbService(Func<ChatDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<OllamaConfig?> GetOllamaConfigAsync()
        {
            using var context = _contextFactory();
            return await context.OllamaConfigs.FirstOrDefaultAsync();
        }

        public async Task<OllamaConfig> SaveOllamaConfigAsync(OllamaConfig config)
        {
            using var context = _contextFactory();
            var existing = await context.OllamaConfigs.FirstOrDefaultAsync();

            if (existing == null)
            {
                context.OllamaConfigs.Add(config);
            }
            else
            {
                existing.InstallPath = config.InstallPath;
                existing.ModelsPath = config.ModelsPath;
                existing.LastUpdated = DateTime.Now;
                config = existing;
            }

            await context.SaveChangesAsync();
            return config;
        }

        public async Task<ChatSession> CreateSessionAsync(string title)
        {
            using var context = _contextFactory();
            var session = new ChatSession
            {
                Title = title,
                CreatedAt = DateTime.Now
            };

            context.ChatSessions.Add(session);
            await context.SaveChangesAsync();
            return session;
        }

        public async Task<ChatSession?> GetSessionAsync(string id)
        {
            using var context = _contextFactory();
            return await context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<ChatSession>> GetAllSessionsAsync()
        {
            using var context = _contextFactory();
            return await context.ChatSessions
                .Include(s => s.Messages)
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(string sessionId, string content, bool isUser)
        {
            using var context = _contextFactory();
            var message = new ChatMessage
            {
                SessionId = sessionId,
                Content = content,
                IsUser = isUser,
                CreatedAt = DateTime.Now
            };

            context.ChatMessages.Add(message);
            await context.SaveChangesAsync();
            return message;
        }

        public async Task DeleteSessionAsync(string id)
        {
            using var context = _contextFactory();
            var session = await context.ChatSessions.FindAsync(id);
            if (session != null)
            {
                context.ChatSessions.Remove(session);
                await context.SaveChangesAsync();
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            using var context = _contextFactory();
            return await context.SaveChangesAsync();
        }
    }
}