using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using ollez.Models;
using ollez.Services;

namespace ollez.ViewModels
{
    public class ChatViewModel : BindableBase
    {
        private readonly IChatService _chatService;
        private string _inputMessage;
        private bool _isProcessing;

        public ObservableCollection<ChatMessage> Messages { get; } = new();
        
        public string InputMessage
        {
            get => _inputMessage;
            set => SetProperty(ref _inputMessage, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public ICommand SendMessageCommand { get; }

        public ChatViewModel(IChatService chatService)
        {
            _chatService = chatService;
            SendMessageCommand = new DelegateCommand(async () => await SendMessageAsync(), CanSendMessage);
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(InputMessage) && !IsProcessing;
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputMessage) || IsProcessing)
                return;

            var userMessage = new ChatMessage
            {
                Content = InputMessage.Trim(),
                IsUser = true
            };

            Messages.Add(userMessage);
            var message = InputMessage;
            InputMessage = string.Empty;
            IsProcessing = true;

            try
            {
                var response = await _chatService.SendMessageAsync(message);
                Messages.Add(new ChatMessage
                {
                    Content = response,
                    IsUser = false
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
} 