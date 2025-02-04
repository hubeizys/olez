using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Serilog;
using System.Windows.Data;
using ollez.Models;

namespace ollez.Services
{
    public class LogService : ILogService
    {
        private FileSystemWatcher? _watcher;
        private StreamReader? _reader;
        private bool _isMonitoring;
        private readonly string _logDirectory;
        private static readonly Regex LogEntryPattern = new(@"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+\[(\w+)\]\s+(.+)$");
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastProcessTime = DateTime.MinValue;
        private const int MAX_LOG_ENTRIES = 1000; // 最大保留的日志条数
        private const string LOG_FILE_PREFIX = "app_"; // 监控的日志前缀
        private long _lastPosition = 0;

        public ObservableCollection<LogEntry> LogEntries { get; }
        public string CurrentLogFile { get; private set; } = string.Empty;

        public LogService()
        {
            Log.Debug("LogService: 构造函数被调用");
            LogEntries = new ObservableCollection<LogEntry>();
            // 启用集合的线程同步
            BindingOperations.EnableCollectionSynchronization(LogEntries, new object());
            
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Log.Debug("LogService: 日志目录路径: {DirectoryPath}", _logDirectory);
            
            if (!Directory.Exists(_logDirectory))
            {
                Log.Debug("LogService: 创建日志目录");
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private string FindLatestLogFile()
        {
            Log.Debug("LogService: 查找最新日志文件");
            var logFiles = Directory.GetFiles(_logDirectory, $"{LOG_FILE_PREFIX}*.log")
                                  .OrderByDescending(f => File.GetLastWriteTime(f))
                                  .ToList();
            Log.Debug("LogService: 找到 {Count} 个日志文件", logFiles.Count);
            return logFiles.FirstOrDefault() ?? string.Empty;
        }

        public void StartMonitoring()
        {
            Log.Debug("LogService: StartMonitoring被调用");
            if (_isMonitoring)
            {
                Log.Debug("LogService: 已经在监控中，跳过");
                return;
            }
            _isMonitoring = true;

            CurrentLogFile = FindLatestLogFile();
            Log.Debug("LogService: 当前日志文件: {FilePath}", CurrentLogFile);
            
            if (string.IsNullOrEmpty(CurrentLogFile))
            {
                Log.Debug("LogService: 未找到日志文件");
                return;
            }

            // 初始加载现有日志内容
            LoadExistingLogs();

            // 设置文件监视
            _watcher = new FileSystemWatcher(_logDirectory)
            {
                Filter = $"{LOG_FILE_PREFIX}*.log",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            Log.Debug("LogService: 文件监视器已设置");

            _watcher.Changed += async (s, e) =>
            {
                if (e.FullPath != CurrentLogFile) return;

                bool acquired = false;
                try
                {
                    acquired = await _semaphore.WaitAsync(0);
                    if (!acquired) return;

                    var now = DateTime.Now;
                    if ((now - _lastProcessTime).TotalMilliseconds < 100)
                    {
                        return;
                    }
                    _lastProcessTime = now;

                    await ReadNewLines();
                }
                finally
                {
                    if (acquired)
                    {
                        _semaphore.Release();
                    }
                }
            };

            _watcher.Created += async (s, e) =>
            {
                bool acquired = false;
                try
                {
                    acquired = await _semaphore.WaitAsync(0);
                    if (!acquired) return;
                    
                    await Task.Delay(500);
                    
                    if (File.Exists(e.FullPath) && 
                        Path.GetFileName(e.FullPath).StartsWith(LOG_FILE_PREFIX, StringComparison.OrdinalIgnoreCase) &&
                        File.GetLastWriteTime(e.FullPath) > File.GetLastWriteTime(CurrentLogFile))
                    {
                        _lastPosition = 0;
                        CurrentLogFile = e.FullPath;
                        LogEntries.Clear(); // 新日志文件，清空旧内容
                        LoadExistingLogs();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "处理新日志文件时出错");
                }
                finally
                {
                    if (acquired)
                    {
                        _semaphore.Release();
                    }
                }
            };
        }

        private void LoadExistingLogs()
        {
            Log.Debug("LogService: 开始加载现有日志");
            try
            {
                var lines = new List<string>();
                using (var stream = new FileStream(CurrentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }

                    _lastPosition = stream.Position;
                }

                // 只保留最后1000行
                if (lines.Count > MAX_LOG_ENTRIES)
                {
                    lines = lines.Skip(lines.Count - MAX_LOG_ENTRIES).ToList();
                }

                var entries = new List<LogEntry>();
                foreach (var line in lines)
                {
                    var match = LogEntryPattern.Match(line);
                    if (match.Success)
                    {
                        entries.Add(new LogEntry(
                            DateTime.Parse(match.Groups[1].Value),
                            match.Groups[3].Value,
                            match.Groups[2].Value
                        ));
                    }
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    LogEntries.Clear();
                    foreach (var entry in entries)
                    {
                        LogEntries.Add(entry);
                    }
                });

                Log.Debug("LogService: 已加载 {Count} 条现有日志记录", LogEntries.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LogService: 加载现有日志时出错");
            }
        }

        private async Task ReadNewLines()
        {
            if (!_isMonitoring) return;
            
            Log.Debug("LogService: 开始读取新日志行");
            try
            {
                var newEntries = new List<LogEntry>();
                using (var stream = new FileStream(CurrentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // 如果文件被截断了，从头开始读取
                    if (stream.Length < _lastPosition)
                    {
                        _lastPosition = 0;
                        LogEntries.Clear();
                    }

                    stream.Position = _lastPosition;
                    using (var reader = new StreamReader(stream))
                    {
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            var match = LogEntryPattern.Match(line);
                            if (match.Success)
                            {
                                newEntries.Add(new LogEntry(
                                    DateTime.Parse(match.Groups[1].Value),
                                    match.Groups[3].Value,
                                    match.Groups[2].Value
                                ));
                            }
                        }
                        _lastPosition = stream.Position;
                    }
                }

                if (newEntries.Count > 0)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var entry in newEntries)
                        {
                            LogEntries.Add(entry);
                        }

                        while (LogEntries.Count > MAX_LOG_ENTRIES)
                        {
                            var removeCount = Math.Min(LogEntries.Count - MAX_LOG_ENTRIES, 100);
                            for (int i = 0; i < removeCount; i++)
                            {
                                LogEntries.RemoveAt(0);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LogService: 读取新日志行时出错");
            }
        }

        public void StopMonitoring()
        {
            Log.Debug("LogService: 停止监控");
            _isMonitoring = false;
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
            _reader?.Dispose();
            _reader = null;
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopMonitoring();
                    _semaphore?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
