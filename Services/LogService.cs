using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

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

        public ObservableCollection<LogEntry> LogEntries { get; }
        public string CurrentLogFile { get; private set; }

        public LogService()
        {
            LogEntries = new ObservableCollection<LogEntry>();
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private string FindLatestLogFile()
        {
            var logFiles = Directory.GetFiles(_logDirectory, "app*.log")
                                  .OrderByDescending(f => File.GetLastWriteTime(f))
                                  .ToList();
            return logFiles.FirstOrDefault();
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;

            CurrentLogFile = FindLatestLogFile();
            if (string.IsNullOrEmpty(CurrentLogFile))
            {
                return;
            }

            // 初始加载现有日志内容
            LoadExistingLogs();

            // 设置文件监视
            _watcher = new FileSystemWatcher(_logDirectory)
            {
                Filter = "app*.log",
                EnableRaisingEvents = true
            };

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
                    System.Diagnostics.Debug.WriteLine($"处理新日志文件时出错: {ex.Message}");
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
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LogEntries.Clear();
                });

                var lastLines = File.ReadAllLines(CurrentLogFile)
                                  .TakeLast(100) // 只加载最后100行
                                  .Select(ParseLogLine)
                                  .Where(entry => entry != null);

                foreach (var entry in lastLines)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(entry);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载现有日志时出错: {ex.Message}");
            }
        }

        private async Task ReadNewLines()
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (_isMonitoring && retryCount <= maxRetries)
            {
                try
                {
                    if (_reader == null)
                    {
                        await Task.Delay(100); // 等待文件解锁
                        var fs = new FileStream(CurrentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        _reader = new StreamReader(fs);
                        
                        // 检查文件是否被截断
                        if (fs.Length < _reader.BaseStream.Position)
                        {
                            _reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        }
                        else if (_reader.BaseStream.Position == 0)
                        {
                            _reader.BaseStream.Seek(0, SeekOrigin.End);
                        }
                    }

                    string line;
                    while (_isMonitoring && (line = await _reader.ReadLineAsync()) != null)
                    {
                        var entry = ParseLogLine(line);
                        if (entry != null)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                LogEntries.Add(entry);
                                // 保持最新的1000条记录
                                while (LogEntries.Count > 1000)
                                {
                                    LogEntries.RemoveAt(0);
                                }
                            });
                        }
                        retryCount = 0; // 成功读取后重置重试计数
                    }

                    // 正常读取完成后等待一小段时间
                    if (_isMonitoring)
                    {
                        await Task.Delay(50);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"读取新日志行时出错: {ex.Message}");
                    _reader?.Dispose();
                    _reader = null;

                    retryCount++;
                    if (retryCount <= maxRetries)
                    {
                        await Task.Delay(Math.Min(100 * retryCount, 1000)); // 指数退避，最大1秒
                    }
                }
            }
        }

        private LogEntry ParseLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            try
            {
                // 尝试匹配 Serilog 的默认输出格式
                var match = Regex.Match(line, @"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+\[(\w+)\]\s+(.+)$");
                if (!match.Success)
                {
                    // 尝试匹配简单格式
                    match = LogEntryPattern.Match(line);
                    if (!match.Success) return null;
                }

                if (DateTime.TryParse(match.Groups[1].Value, out DateTime timestamp))
                {
                    return new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = match.Groups[2].Value,
                        Message = match.Groups[3].Value.Trim()
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析日志行时出错: {ex.Message}");
            }

            return null;
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            _watcher?.Dispose();
            _reader?.Dispose();
            _watcher = null;
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