using System;
using System.Collections.ObjectModel;
using ollez.Models;

namespace ollez.Services
{
   

    public interface ILogService
    {
        ObservableCollection<LogEntry> LogEntries { get; }
        string CurrentLogFile { get; }
        void StartMonitoring();
        void StopMonitoring();
    }
}