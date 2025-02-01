using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using ollez.Models;
using ollez.Services;
using System.Diagnostics;
using Serilog;
using System.Windows;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace ollez.ViewModels
{
    public class ChatViewModel : BindableBase
    {

        private StringBuilder _contentBuilder = new StringBuilder();
        private readonly IChatService _chatService;
        private readonly ISystemCheckService _systemCheckService;
        private string _inputMessage;
        private bool _isProcessing;
        private string _selectedModel;
        private ObservableCollection<string> _availableModels;
        private ObservableCollection<ChatMessage> _messages;
        private FlowDocument _testDocument;

        private string _testContent;

        public string TestContent
        {
            get => _testContent;
            set => SetProperty(ref _testContent, value);
        }

        public FlowDocument TestDocument
        {
            get => _testDocument;
            set => SetProperty(ref _testDocument, value);
        }

        public ObservableCollection<ChatMessage> Messages
        {
            get => _messages;
            private set => SetProperty(ref _messages, value);
        }

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

            Messages = new ObservableCollection<ChatMessage>();
            AvailableModels = new ObservableCollection<string>();
            TestDocument = new FlowDocument();
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

        private async Task UpdateUIAsync(string chunk)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (TestDocument.Blocks.Count == 0)
                {
                    TestDocument.Blocks.Add(new Paragraph());
                }

                var paragraph = TestDocument.Blocks.FirstBlock as Paragraph;
                paragraph.Inlines.Add(new Run(chunk));
                RaisePropertyChanged(nameof(TestDocument));
                Log.Information($"[ChatViewModel] 更新文档内容，添加chunk: '{chunk}'");
            });
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputMessage) || IsProcessing || string.IsNullOrEmpty(SelectedModel))
            {
                Log.Error($"[ChatViewModel] 无法发送消息: InputMessage为空={string.IsNullOrWhiteSpace(InputMessage)}, IsProcessing={IsProcessing}, SelectedModel为空={string.IsNullOrEmpty(SelectedModel)}");
                return;
            }

            Log.Information($"[ChatViewModel] 开始发送消息: Model={SelectedModel}, Message={InputMessage}");
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
                Log.Information("[ChatViewModel] 创建助手消息");
                var assistantMessage = new ChatMessage
                {
                    Content = string.Empty,
                    IsUser = false
                };
                Messages.Add(assistantMessage);

                Log.Information("[ChatViewModel] 开始获取流式响应");
                var responseStream = await _chatService.SendMessageStreamAsync(message, SelectedModel);
                IsProcessing = false; // 在开始处理流式响应前关闭加载指示器

                Log.Information("[ChatViewModel] 开始处理流式响应");
                await foreach (var chunk in responseStream)
                {
                    Log.Information($"[ChatViewModel] 收到chunk内容: '{chunk}'");
                    await UpdateUIAsync(chunk);
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        // 确保更新 Content 属性是在 UI 线程上执行的
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                  
                            assistantMessage.Content += chunk;
                            var index = Messages.IndexOf(assistantMessage);
                            Messages[index] = null;  // 触发集合变化
                            Messages[index] = assistantMessage;
                            Log.Information($"[ChatViewModel] 当前完整消息: '{assistantMessage.Content.Length}' 字符");

                        });
                    }
                }
                Log.Information("[ChatViewModel] 流式响应处理完成");
            }
            catch (Exception ex)
            {
                Log.Error($"[ChatViewModel] 发生错误: {ex}");
                Messages.Add(new ChatMessage
                {
                    Content = $"发生错误: {ex.Message}",
                    IsUser = false
                });
            }
            finally
            {
                IsProcessing = false;
                Log.Information("[ChatViewModel] 消息处理完成");
            }
        }

    }
}