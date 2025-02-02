using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace ollez.Models
{
    public class ChatSession
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "新会话";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();
    }
}