using Microsoft.EntityFrameworkCore;
using ollez.Data;
using ollez.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ollez.Services
{
    public class ChatDbService : IChatDbService
    {
        private readonly ChatDbContext _context;

        public ChatDbService(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<DbChatSession> CreateSessionAsync(string title)
        {
            var session = new DbChatSession
            {
                Title = title,
                CreatedAt = DateTime.Now
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<DbChatSession?> GetSessionAsync(string id)
        {
            return await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<DbChatSession>> GetAllSessionsAsync()
        {
            return await _context.ChatSessions
                .Include(s => s.Messages)
                .ToListAsync();
        }

        public async Task<DbChatMessage> AddMessageAsync(string sessionId, string content, bool isUser)
        {
            var message = new DbChatMessage
            {
                SessionId = sessionId,
                Content = content,
                IsUser = isUser,
                CreatedAt = DateTime.Now
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task DeleteSessionAsync(string id)
        {
            var session = await _context.ChatSessions.FindAsync(id);
            if (session != null)
            {
                _context.ChatSessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}