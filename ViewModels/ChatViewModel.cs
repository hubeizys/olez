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
using System.Threading;
using Serilog.Core;
using Serilog.Events;
using ollez.Attributes;

namespace ollez.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        [Logger("ui_debug", MinimumLevel = LogEventLevel.Debug)]
        private readonly ILogger _debugLogger = null!;

        private StringBuilder _contentBuilder = new();
        private readonly IChatService _chatService;
        private readonly ISystemCheckService _systemCheckService;
        private string _inputMessage = string.Empty;
        private bool _isProcessing;
        private string _selectedModel = string.Empty;
        private ObservableCollection<string> _availableModels = new();
        private ObservableCollection<ChatMessage> _messages = new();
        private StringBuilder _pendingContent = new();
        private DateTime _lastUpdateTime = DateTime.Now;
        private const int UI_UPDATE_INTERVAL_MS = 200;
        private ObservableCollection<ChatSession> _chatSessions = new();
        private ChatSession _currentSession = new()
        {
            Id = string.Empty,
            Title = string.Empty,
            CreatedAt = DateTime.Now,
            Messages = new ObservableCollection<ChatMessage>()
        };

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

        public ObservableCollection<ChatSession> ChatSessions
        {
            get => _chatSessions;
            private set => SetProperty(ref _chatSessions, value);
        }

        public ChatSession CurrentSession
        {
            get => _currentSession;
            set
            {
                if (SetProperty(ref _currentSession, value))
                {
                    if (value != null)
                    {
                        _chatService.SetCurrentSessionId(value.Id);
                        Messages = new ObservableCollection<ChatMessage>(value.Messages);
                    }
                }
            }
        }

        public ICommand SendMessageCommand { get; }
        public ICommand RefreshModelsCommand { get; }
        public ICommand NewSessionCommand { get; }
        public ICommand NewChatCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        public ChatViewModel(IChatService chatService, ISystemCheckService systemCheckService)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _systemCheckService = systemCheckService ?? throw new ArgumentNullException(nameof(systemCheckService));
            
            SendMessageCommand = new DelegateCommand(async () => await SendMessageAsync(), CanSendMessage);
            RefreshModelsCommand = new DelegateCommand(async () => await RefreshModelsAsync());
            NewSessionCommand = new DelegateCommand(async () => await CreateNewSessionAsync());
            NewChatCommand = new DelegateCommand(ExecuteNewChat);
            OpenSettingsCommand = new DelegateCommand(ExecuteOpenSettings);
            
            InitializeAsync().ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    Log.Error($"初始化失败: {task.Exception}");
                }
            });
        }

        private async Task InitializeAsync()
        {
            await RefreshModelsAsync();
            await CreateNewSessionAsync();
        }

        private async Task CreateNewSessionAsync()
        {
            try
            {
                var sessionId = await _chatService.CreateNewSession();
                if (string.IsNullOrEmpty(sessionId))
                {
                    throw new InvalidOperationException("创建会话失败：会话ID为空");
                }
                
                var newSession = new ChatSession
                {
                    Id = sessionId,
                    Title = "新会话",
                    CreatedAt = DateTime.Now,
                    Messages = new ObservableCollection<ChatMessage>()
                };
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ChatSessions.Add(newSession);
                    CurrentSession = newSession;
                });
                
                _debugLogger.Information($"[ChatViewModel] 创建新会话成功: {sessionId}");
                _debugLogger.Information($"[ChatViewModel] 当前会话: {CurrentSession.Id}");
                Log.Information($"[ChatViewModel] 创建新会话成功: {sessionId}");
            }
            catch (Exception ex)
            {
                Log.Error($"[ChatViewModel] 创建新会话失败: {ex}");
                throw;
            }
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
            _debugLogger.Information($"[UI Debug] 检查是否可以发送消息: InputMessage={InputMessage}, IsProcessing={IsProcessing}, " +
                                     $"SelectedModel={SelectedModel}, CurrentSession={CurrentSession}");
            return !string.IsNullOrWhiteSpace(InputMessage) && !IsProcessing && 
                   !string.IsNullOrEmpty(SelectedModel) && CurrentSession != null;
        }

        private async Task UpdateUIAsync(string chunk)
        {
            _debugLogger.Information($"[UI Debug] 收到新的chunk: '{chunk}'");
            _pendingContent.Append(chunk);
            _debugLogger.Information($"[UI Debug] 当前缓冲区大小: {_pendingContent.Length} 字符");
            
            var now = DateTime.Now;
            var timeSinceLastUpdate = (now - _lastUpdateTime).TotalMilliseconds;
            _debugLogger.Information($"[UI Debug] 距离上次更新过去了: {timeSinceLastUpdate}ms");

            if (timeSinceLastUpdate >= UI_UPDATE_INTERVAL_MS)
            {
                _debugLogger.Information($"[UI Debug] 准备更新UI，当前缓冲区内容长度: {_pendingContent.Length}");
                
                try 
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (Messages.Count > 0)
                        {
                            var lastMessage = Messages.Last();
                            if (!lastMessage.IsUser)
                            {
                                lastMessage.Content += _pendingContent.ToString();
                                RaisePropertyChanged(nameof(Messages));
                            }
                        }
                    });

                    _pendingContent.Clear();
                    _lastUpdateTime = now;
                }
                catch (Exception ex)
                {
                    _debugLogger.Error($"[UI Debug] 更新UI时发生错误: {ex}");
                }
            }
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputMessage) || IsProcessing || 
                string.IsNullOrEmpty(SelectedModel) || CurrentSession == null)
            {
                _debugLogger.Error($"[UI Debug] 无法发送消息: InputMessage为空={string.IsNullOrWhiteSpace(InputMessage)}, " +
                                 $"IsProcessing={IsProcessing}, SelectedModel为空={string.IsNullOrEmpty(SelectedModel)}, " +
                                 $"CurrentSession为空={CurrentSession == null}");
                return;
            }

            try
            {
                _pendingContent.Clear();
                _lastUpdateTime = DateTime.Now;

                Log.Information($"[ChatViewModel] 开始发送消息: Model={SelectedModel}, Message={InputMessage}");
                var userMessage = new ChatMessage
                {
                    Content = InputMessage.Trim(),
                    IsUser = true
                };

                Messages.Add(userMessage);
                CurrentSession.Messages.Add(userMessage);
                
                var message = InputMessage;
                InputMessage = string.Empty;
                IsProcessing = true;

                Log.Information("[ChatViewModel] 创建助手消息");
                var assistantMessage = new ChatMessage
                {
                    Content = string.Empty,
                    IsUser = false,
                    IsThinking = true
                };
                Messages.Add(assistantMessage);
                CurrentSession.Messages.Add(assistantMessage);

                Log.Information("[ChatViewModel] 开始获取流式响应");
                var responseStream = await _chatService.SendMessageStreamAsync(message, SelectedModel);
                assistantMessage.IsThinking = false;
                IsProcessing = false;

                Log.Information("[ChatViewModel] 开始处理流式响应");
                await Task.Run(async () =>
                {
                    await foreach (var chunk in responseStream)
                    {
                        Log.Information($"[ChatViewModel] 收到chunk内容: '{chunk}'");
                        await UpdateUIAsync(chunk);
                    }
                });
                Log.Information("[ChatViewModel] 流式响应处理完成");
            }
            catch (Exception ex)
            {
                Log.Error($"[ChatViewModel] 发生错误: {ex}");
                var errorMessage = new ChatMessage
                {
                    Content = $"发生错误: {ex.Message}",
                    IsUser = false
                };
                Messages.Add(errorMessage);
                CurrentSession.Messages.Add(errorMessage);
            }
            finally
            {
                IsProcessing = false;
                Log.Information("[ChatViewModel] 消息处理完成");
            }
        }

        private void ExecuteNewChat()
        {
            // 清空当前对话
            Messages.Clear();
            CurrentSession.Messages.Clear();
            CurrentSession.Title = "新会话";
            CurrentSession.CreatedAt = DateTime.Now;
            CurrentSession.Id = Guid.NewGuid().ToString();
        }

        private void ExecuteOpenSettings()
        {
            // TODO: 实现打开设置的逻辑
            MessageBox.Show("设置功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}