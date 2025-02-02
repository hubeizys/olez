using System;
using System.Collections.ObjectModel;

namespace ollez.Services
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
    }

    public interface ILogService
    {
        ObservableCollection<LogEntry> LogEntries { get; }
        string CurrentLogFile { get; }
        void StartMonitoring();
        void StopMonitoring();
    }
}