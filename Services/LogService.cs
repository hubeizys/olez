using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Serilog;

namespace ollez.Services
{
    public class LogService : ILogService
    {
        private FileSystemWatcher _watcher;
        private StreamReader _reader;
        private bool _isMonitoring;
        private readonly string _logDirectory;
        private static readonly Regex LogEntryPattern = new(@"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+\[(\w+)\]\s+(.+)$");
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastProcessTime = DateTime.MinValue;
        private const string APP_LOG_PREFIX = "app_";  // 应用程序自身的日志前缀
        private const string MONITOR_LOG_PREFIX = "monitor_";  // 要监控的日志前缀

        public ObservableCollection<LogEntry> LogEntries { get; }
        public string CurrentLogFile { get; private set; }

        public LogService()
        {
            Log.Debug("LogService: 构造函数被调用");
            LogEntries = new ObservableCollection<LogEntry>();
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Log.Debug("LogService: 日志目录路径: {DirectoryPath}", _logDirectory);
            
            if (!Directory.Exists(_logDirectory))
            {
                Log.Debug("LogService: 创建日志目录");
                Directory.CreateDirectory(_logDirectory);
            }

            // 如果没有监控日志文件，创建一个示例文件
            EnsureMonitorLogFileExists();
        }

        private void EnsureMonitorLogFileExists()
        {
            var defaultLogFile = Path.Combine(_logDirectory, $"{MONITOR_LOG_PREFIX}default.log");
            if (!File.Exists(defaultLogFile))
            {
                Log.Debug("LogService: 创建示例日志文件");
                try
                {
                    using (var writer = File.CreateText(defaultLogFile))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] 系统启动");
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] 这是一个示例日志文件");
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [WARN] 这是一条警告消息");
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] 这是一条错误消息");
                    }
                    Log.Debug("LogService: 示例日志文件创建成功");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "LogService: 创建示例日志文件时出错");
                }
            }
        }

        private string FindLatestLogFile()
        {
            Log.Debug("LogService: 查找最新日志文件");
            var logFiles = Directory.GetFiles(_logDirectory, $"{MONITOR_LOG_PREFIX}*.log")  // 只监控指定前缀的日志
                                  .OrderByDescending(f => File.GetLastWriteTime(f))
                                  .ToList();
            Log.Debug("LogService: 找到 {Count} 个日志文件", logFiles.Count);
            
            var result = logFiles.FirstOrDefault();
            if (string.IsNullOrEmpty(result))
            {
                // 如果没有找到日志文件，确保创建一个
                EnsureMonitorLogFileExists();
                // 重新查找
                result = Directory.GetFiles(_logDirectory, $"{MONITOR_LOG_PREFIX}*.log")
                                .OrderByDescending(f => File.GetLastWriteTime(f))
                                .FirstOrDefault();
            }
            return result;
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
                Filter = $"{MONITOR_LOG_PREFIX}*.log",  // 只监控指定前缀的日志
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
            };

            Log.Debug("LogService: 文件监视器已设置");

            _watcher.Changed += async (s, e) =>
            {
                if (e.FullPath != CurrentLogFile) return;

                try
                {
                    await _semaphore.WaitAsync();
                    
                    // 防抖动：如果距离上次处理时间太近，就跳过
                    if ((DateTime.Now - _lastProcessTime).TotalMilliseconds < 100)
                    {
                        return;
                    }
                    _lastProcessTime = DateTime.Now;

                    await ReadNewLines();
                }
                finally
                {
                    _semaphore.Release();
                }
            };

            _watcher.Created += async (s, e) =>
            {
                try
                {
                    await _semaphore.WaitAsync();

                    // 等待新文件完全写入
                    await Task.Delay(500);
                    
                    if (File.Exists(e.FullPath) && 
                        File.GetLastWriteTime(e.FullPath) > File.GetLastWriteTime(CurrentLogFile))
                    {
                        CurrentLogFile = e.FullPath;
                        LoadExistingLogs();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "处理新日志文件时出错");
                }
                finally
                {
                    _semaphore.Release();
                }
            };

            // 开始监视文件末尾的新行
            _ = ReadNewLines();
        }

        private void LoadExistingLogs()
        {
            Log.Debug("LogService: 开始加载现有日志");
            try
            {
                using (var stream = new FileStream(CurrentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    int count = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = LogEntryPattern.Match(line);
                        if (match.Success)
                        {
                            var entry = new LogEntry
                            {
                                Timestamp = DateTime.Parse(match.Groups[1].Value),
                                Level = match.Groups[2].Value,
                                Message = match.Groups[3].Value
                            };
                            LogEntries.Add(entry);
                            count++;
                        }
                    }
                    Log.Debug("LogService: 已加载 {Count} 条现有日志记录", count);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LogService: 加载现有日志时出错");
            }
        }

        private async Task ReadNewLines()
        {
            Log.Debug("LogService: 开始读取新日志行");
            try
            {
                using (var stream = new FileStream(CurrentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    stream.Seek(0, SeekOrigin.End);
                    
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        var match = LogEntryPattern.Match(line);
                        if (match.Success)
                        {
                            var entry = new LogEntry
                            {
                                Timestamp = DateTime.Parse(match.Groups[1].Value),
                                Level = match.Groups[2].Value,
                                Message = match.Groups[3].Value
                            };
                            App.Current.Dispatcher.Invoke(() => LogEntries.Add(entry));
                            Log.Debug("LogService: 添加新日志 - {Timestamp} [{Level}] {Message}", 
                                entry.Timestamp, entry.Level, entry.Message);
                        }
                    }
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
            _watcher?.Dispose();
            _watcher = null;
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