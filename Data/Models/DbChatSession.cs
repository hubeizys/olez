using System;
using System.Collections.Generic;

namespace ollez.Data.Models
{
    public class DbChatSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // 导航属性
        public ICollection<DbChatMessage> Messages { get; set; } = new List<DbChatMessage>();
    }
}