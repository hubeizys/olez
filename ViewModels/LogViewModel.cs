using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using ollez.Services;

namespace ollez.ViewModels
{
    public class LogViewModel : BindableBase, INavigationAware
    {
        private readonly ILogService _logService;
        public event EventHandler ScrollToEndRequested;
        
        public ObservableCollection<LogEntry> LogEntries => _logService.LogEntries;

        private string _currentLogFile;
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

        public LogViewModel(ILogService logService)
        {
            _logService = logService;
            ClearCommand = new DelegateCommand(() => LogEntries.Clear());
            
            // 监听日志更新以触发滚动
            LogEntries.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && AutoScroll)
                {
                    ScrollToEndRequested?.Invoke(this, System.EventArgs.Empty);
                }
            };
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            CurrentLogFile = _logService.CurrentLogFile;
            _logService.StartMonitoring();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _logService.StopMonitoring();
        }
    }
}