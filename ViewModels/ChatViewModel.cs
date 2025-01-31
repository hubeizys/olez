using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using ollez.Models;
using ollez.Services;

namespace ollez.ViewModels
{
    public class ChatViewModel : BindableBase
    {
        private readonly IChatService _chatService;
        private readonly ISystemCheckService _systemCheckService;
        private string _inputMessage;
        private bool _isProcessing;
        private string _selectedModel;
        private ObservableCollection<string> _availableModels;

        public ObservableCollection<ChatMessage> Messages { get; } = new();
        
        public ObservableCollection<string> AvailableModels
        {
            get => _availableModels;
            private set => SetProperty(ref _availableModels, value);
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set => SetProperty(ref _selectedModel, value);
        }

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
        public ICommand RefreshModelsCommand { get; }

        public ChatViewModel(IChatService chatService, ISystemCheckService systemCheckService)
        {
            _chatService = chatService;
            _systemCheckService = systemCheckService;
            SendMessageCommand = new DelegateCommand(async () => await SendMessageAsync(), CanSendMessage);
            RefreshModelsCommand = new DelegateCommand(async () => await RefreshModelsAsync());
            
            AvailableModels = new ObservableCollection<string>();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await RefreshModelsAsync();
        }

        private async Task RefreshModelsAsync()
        {
            var ollamaInfo = await _systemCheckService.CheckOllamaAsync();
            if (ollamaInfo.IsRunning && ollamaInfo.InstalledModels != null)
            {
                var models = ollamaInfo.InstalledModels.Select(m => m.Name).OrderBy(n => n).ToList();
                AvailableModels.Clear();
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }

                if (string.IsNullOrEmpty(SelectedModel) && AvailableModels.Any())
                {
                    SelectedModel = AvailableModels.First();
                }
            }
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(InputMessage) && !IsProcessing && !string.IsNullOrEmpty(SelectedModel);
        }

         private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputMessage) || IsProcessing || string.IsNullOrEmpty(SelectedModel))
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
                var assistantMessage = new ChatMessage
                {
                    Content = string.Empty,
                    IsUser = false
                };
                Messages.Add(assistantMessage);

                var responseStream = await _chatService.SendMessageStreamAsync(message, SelectedModel);
                await foreach (var chunk in responseStream)
                {
                    assistantMessage.Content += chunk;
                    RaisePropertyChanged(nameof(Messages)); // 通知 UI 更新
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
} 