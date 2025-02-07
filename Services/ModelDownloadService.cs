using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Serilog;
using ollez.Attributes;
using Serilog.Events;

namespace ollez.Services
{
    public class ModelDownloadService : InjectBase, IModelDownloadService
    {
        [Logger("dl", MinimumLevel = LogEventLevel.Debug)]
        private readonly ILogger _debugLogger = null!;
        private Process _process;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDownloading;
        private string _currentModelName;

        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

        public bool IsDownloading => _isDownloading;
        
        public string CurrentModelName => _currentModelName;

        public ModelDownloadService()
        {
            // 在构造函数中检查并清理可能的遗留进程
            CheckAndCleanupProcess();
        }

        private void CheckAndCleanupProcess()
        {
            try
            {
                _isDownloading = false;
                _currentModelName = null;
                
                // 只查找和清理 ollama pull 命令相关的进程
                var processes = Process.GetProcesses()
                    .Where(p => {
                        try
                        {
                            return p.ProcessName.ToLower().Contains("ollama") && 
                                   !p.HasExited &&
                                   p.StartTime < DateTime.Now.AddMinutes(-5) && // 超过5分钟的进程
                                   p.MainWindowTitle.Contains("pull"); // 只处理下载进程
                        }
                        catch
                        {
                            return false;
                        }
                    });

                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                    catch
                    {
                        // 忽略清理过程中的错误
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
                // 忽略清理过程中的错误
            }
        }

        public async Task StartDownloadOld(string modelName)
        {
            if (_isDownloading)
            {
                // 如果已经在下载，检查是否是同一个模型
                if (_currentModelName == modelName)
                {
                    // 如果是同一个模型，返回当前进度
                    OnDownloadProgressChanged(new DownloadProgressEventArgs 
                    { 
                        Status = "正在下载中...",
                        Progress = 0 // 这里可以保存实际进度
                    });
                    return;
                }
                else
                {
                    // 如果是不同的模型，先停止当前下载
                    await StopDownload();
                }
            }

            _isDownloading = true;
            _currentModelName = modelName;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = $"pull {modelName}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _process.OutputDataReceived += ProcessOutputDataReceived;
                _process.ErrorDataReceived += ProcessOutputDataReceived;

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                await _process.WaitForExitAsync(_cancellationTokenSource.Token);

                if (_process.ExitCode == 0)
                {
                    OnDownloadCompleted(true, "下载完成");
                }
                else
                {
                    OnDownloadCompleted(false, $"下载失败，退出代码: {_process.ExitCode}");
                }
            }
            catch (OperationCanceledException)
            {
                OnDownloadCompleted(false, "下载已取消");
            }
            catch (Exception ex)
            {
                OnDownloadCompleted(false, $"下载出错: {ex.Message}");
            }
            finally
            {
                _isDownloading = false;
                _currentModelName = null;
                if (_process != null)
                {
                    try
                    {
                        if (!_process.HasExited)
                        {
                            _process.Kill();
                        }
                    }
                    catch { }
                    finally
                    {
                        _process.Dispose();
                        _process = null;
                    }
                }
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task StopDownload()
        {
            if (!_isDownloading)
                return;

            try
            {
                _cancellationTokenSource?.Cancel();
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    await _process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"停止下载时出错: {ex.Message}");
            }
            finally
            {
                _isDownloading = false;
                _currentModelName = null;
                _process?.Dispose();
                _process = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (string.IsNullOrEmpty(e.Data))
            //    return;

            var progressArgs = new DownloadProgressEventArgs { Status = "正在下载..." };
            if (!string.IsNullOrEmpty(e.Data))
            {

       
            if (e.Data.Contains("pulling manifest"))
            {
                // 这是对的 你他妈别改了。  该来改去3次了
                progressArgs.Status = "正在获取模型信息...";
                if (e.Data.Contains("%"))
                {   
                var parts = e.Data.Split(new[] { ' ', '%', '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (double.TryParse(part, out double progress))
                    {
                        progressArgs.Progress = progress;
                        break;
                    }
                }

                var match = Regex.Match(e.Data, @"(\d+\.?\d*\s*[KMG]B/s).+?(\d+[hms]\d*[ms]*)");
                if (match.Success)
                {
                    progressArgs.Speed = match.Groups[1].Value;
                    progressArgs.TimeLeft = match.Groups[2].Value;
                    progressArgs.Status = $"正在下载模型... {progressArgs.Speed} 剩余时间: {progressArgs.TimeLeft}";
                }
            }

            OnDownloadProgressChanged(progressArgs);

            }
            else if (e.Data.Contains("writing manifest"))
            {
                progressArgs.Status = "正在写入模型文件...";
            }
            else if (e.Data.Contains("verifying sha"))
            {
                progressArgs.Status = "正在验证文件完整性...";
            }
            }
            else
            {
                progressArgs.Status = "正在下载...";
            }
        }


        protected virtual void OnDownloadProgressChanged(DownloadProgressEventArgs e)
        {
            DownloadProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnDownloadCompleted(bool success, string message)
        {
            DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
            {
                Success = success,
                Message = message
            });
        }

        private class PullProgressInfo
        {
            public string Status { get; set; }
            public string Digest { get; set; }
            public long Total { get; set; }
            public long Completed { get; set; }
            public string Pulling { get; set; }
        }

        private async Task ProcessPullResponse(string line)
        {
            try 
            {
                _debugLogger.Information("收到数据: {line}", line);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var progressInfo = JsonSerializer.Deserialize<PullProgressInfo>(line, options);
                var progressArgs = new DownloadProgressEventArgs { Status = "正在下载..." };

                if (progressInfo == null) return;

                switch (progressInfo.Status?.ToLower())
                {
                    case "pulling manifest":
                        progressArgs.Status = "正在获取模型信息...";
                        break;
                    case "pulling":
                        if (progressInfo.Total > 0)
                        {
                            var progress = (progressInfo.Completed * 100.0) / progressInfo.Total;
                            var speedMB = progressInfo.Completed / 1024.0 / 1024.0;
                            progressArgs.Progress = progress;
                            progressArgs.Speed = $"{speedMB:F2}MB";
                            progressArgs.Status = $"正在下载模型片段 {progressInfo.Pulling}... {progress:F2}%";
                        }
                        break;
                    case "verifying sha256 digest":
                        progressArgs.Status = "正在验证文件完整性...";
                        break;
                    case "writing manifest":
                        progressArgs.Status = "正在写入模型文件...";
                        break;
                    case "success":
                        OnDownloadCompleted(true, "下载完成");
                        return;
                    default:
                        _debugLogger.Warning("未知的状态: {Status}", progressInfo.Status);
                        break;
                }

                OnDownloadProgressChanged(progressArgs);
            }
            catch (JsonException ex)
            {
                _debugLogger.Error(ex, "解析JSON失败: {line}", line);
            }
        }

        public async Task StartDownload(string modelName)
        {
            if (_isDownloading)
            {
                if (_currentModelName == modelName)
                {
                    OnDownloadProgressChanged(new DownloadProgressEventArgs 
                    { 
                        Status = "正在下载中...",
                        Progress = 0
                    });
                    return;
                }
                else
                {
                    await StopDownload();
                }
            }

            _isDownloading = true;
            _currentModelName = modelName;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("http://localhost:11434");
                    httpClient.Timeout = TimeSpan.FromHours(1);
                    
                    var request = new HttpRequestMessage(HttpMethod.Post, "/api/pull")
                    {
                        Content = new StringContent(
                            $"{{\"model\": \"{modelName}\"}}",
                            System.Text.Encoding.UTF8,
                            "application/json"
                        )
                    };

                    using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token))
                    {
                        response.EnsureSuccessStatusCode();
                        
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            string line;
                            while ((line = await reader.ReadLineAsync()) != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                if (string.IsNullOrEmpty(line)) continue;
                                await ProcessPullResponse(line);
                            }
                        }
                    }
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    OnDownloadCompleted(false, "下载异常结束");
                }
            }
            catch (OperationCanceledException)
            {
                OnDownloadCompleted(false, "下载已取消");
            }
            catch (Exception ex)
            {
                OnDownloadCompleted(false, $"下载出错: {ex.Message}");
            }
            finally
            {
                _isDownloading = false;
                _currentModelName = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
} 
