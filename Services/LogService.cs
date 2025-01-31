using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ollez.Services
{
    public class LogService : ILogService
    {
        private FileSystemWatcher _watcher;
        private StreamReader _reader;
        private bool _isMonitoring;
        private readonly string _logDirectory;
        private static readonly Regex LogEntryPattern = new(@"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+\[(\w+)\]\s+(.+)$");

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
                if (e.FullPath == CurrentLogFile)
                {
                    await ReadNewLines();
                }
            };

            _watcher.Created += (s, e) =>
            {
                if (File.GetLastWriteTime(e.FullPath) > File.GetLastWriteTime(CurrentLogFile))
                {
                    CurrentLogFile = e.FullPath;
                    LoadExistingLogs();
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
            try
            {
                if (_reader == null)
                {
                    var fs = new FileStream(CurrentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _reader = new StreamReader(fs);
                    _reader.BaseStream.Seek(fs.Length, SeekOrigin.Begin);
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取新日志行时出错: {ex.Message}");
                _reader?.Dispose();
                _reader = null;
            }
        }

        private LogEntry ParseLogLine(string line)
        {
            try
            {
                var match = LogEntryPattern.Match(line);
                if (match.Success)
                {
                    return new LogEntry
                    {
                        Timestamp = DateTime.Parse(match.Groups[1].Value),
                        Level = match.Groups[2].Value,
                        Message = match.Groups[3].Value
                    };
                }
            }
            catch { }
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
    }
}