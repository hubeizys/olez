using System;

namespace ollez.Data.Models
{
    public class DbChatMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public string Role => IsUser ? "user" : "assistant";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // 外键
        public string SessionId { get; set; } = string.Empty;
        public DbChatSession? Session { get; set; }
    }
}