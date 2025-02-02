using Prism.Mvvm;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Documents;

namespace ollez.Models
{
    public class ChatMessage : BindableBase
    {
        private string _content = string.Empty;
        private bool _isUser;
        private bool _isThinking;
        private string _messageType = "text";
        private FlowDocument _messageDocument;

        public int Id { get; set; }

        public string MessageType
        {
            get => _messageType;
            set => SetProperty(ref _messageType, value);
        }
        
        public string Content
        {
            get => _content;
            set
            {
                if (SetProperty(ref _content, value))
                {
                    MessageDocument = new FlowDocument(new Paragraph(new Run(value)));
                }
            }
        }

        public bool IsUser
        {
            get => _isUser;
            set => SetProperty(ref _isUser, value);
        }

        public bool IsThinking
        {
            get => _isThinking;
            set => SetProperty(ref _isThinking, value);
        }

        [NotMapped]
        public FlowDocument MessageDocument
        {
            get => _messageDocument;
            set => SetProperty(ref _messageDocument, value);
        }

        public string Role => IsUser ? "user" : "assistant";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // 外键
        public string SessionId { get; set; } = string.Empty;
        
        [NotMapped]
        public ChatSession? Session { get; set; }

        public ChatMessage()
        {
            _messageDocument = new FlowDocument();
        }
    }
}