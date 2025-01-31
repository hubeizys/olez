namespace ollez.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsUser { get; set; }
        public string Role => IsUser ? "user" : "assistant";
    }
} 