using Prism.Mvvm;

namespace ollez.Models
{
    public class ChatMessage : BindableBase
    {
        private string _content;
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        private bool _isUser;
        public bool IsUser
        {
            get => _isUser;
            set => SetProperty(ref _isUser, value);
        }

        public string Role => IsUser ? "user" : "assistant";
    }
}