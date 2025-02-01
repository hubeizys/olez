using Prism.Mvvm;
using System.Windows.Documents;

namespace ollez.Models
{
    public class ChatMessage : BindableBase
    {
        private string _content;
        private bool _isUser;
        private bool _isThinking;
        private FlowDocument _messageDocument;

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

        public FlowDocument MessageDocument
        {
            get => _messageDocument;
            set => SetProperty(ref _messageDocument, value);
        }

        public string Role => IsUser ? "user" : "assistant";

        public ChatMessage()
        {
            _messageDocument = new FlowDocument();
        }
    }
}