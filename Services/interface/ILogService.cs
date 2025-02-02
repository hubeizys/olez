using System;
using System.Collections.ObjectModel;

namespace ollez.Services
{
    public class LogEntry
    {
        public DateTime Timestamp { get; init; }
        public string Message { get; init; }
        public string Level { get; init; }

        public LogEntry(DateTime timestamp, string message, string level)
        {
            Timestamp = timestamp;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Level = level ?? throw new ArgumentNullException(nameof(level));
        }
    }

    public interface ILogService
    {
        ObservableCollection<LogEntry> LogEntries { get; }
        string CurrentLogFile { get; }
        void StartMonitoring();
        void StopMonitoring();
    }
}