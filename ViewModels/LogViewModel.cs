using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using ollez.Services;
using Serilog;
using ollez.Models;

namespace ollez.ViewModels
{
    public class LogViewModel : BindableBase, INavigationAware
    {
        private readonly ILogService _logService;
        public event EventHandler? ScrollToEndRequested;


        public ObservableCollection<LogEntry> LogEntries => _logService.LogEntries;

        private string _currentLogFile = string.Empty;
        public string CurrentLogFile
        {
            get => _currentLogFile;
            set => SetProperty(ref _currentLogFile, value);
        }

        public ICommand ClearCommand { get; }

        private bool _autoScroll = true;
        public bool AutoScroll
        {
            get => _autoScroll;
            set => SetProperty(ref _autoScroll, value);
        }

        private bool _isLoggingCollectionChanges = false;
        public bool IsLoggingCollectionChanges
        {
            get => _isLoggingCollectionChanges;
            set => SetProperty(ref _isLoggingCollectionChanges, value);
        }

        public LogViewModel(ILogService logService)
        {
            Log.Debug("LogViewModel: 构造函数被调用");
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));


            ClearCommand = new DelegateCommand(() =>

            {
                Log.Debug("LogViewModel: 清除命令被执行");
                LogEntries.Clear();
                _logService.StartMonitoring();
            });


            LogEntries.CollectionChanged += (s, e) =>
            {
                if (IsLoggingCollectionChanges)
                {
                    Log.Debug("LogViewModel: 集合变更事件触发 - 动作类型: {Action}, 当前日志条数: {Count}", e.Action, LogEntries.Count);
                }


                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && AutoScroll)
                {
                    ScrollToEndRequested?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Log.Debug("LogViewModel: OnNavigatedTo被调用");
            _logService.StartMonitoring();
            CurrentLogFile = _logService.CurrentLogFile;
            Log.Debug("LogViewModel: 当前日志文件路径: {FilePath}", CurrentLogFile);
            Log.Debug("LogViewModel: 当前日志条数: {Count}", LogEntries.Count);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Log.Debug("LogViewModel: OnNavigatedFrom被调用");
            _logService.StopMonitoring();
        }
    }
}