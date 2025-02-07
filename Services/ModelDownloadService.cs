using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ollez.Services
{
    public class ModelDownloadService : IModelDownloadService
    {
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
                
                // 查找可能的遗留ollama pull进程
                var processes = Process.GetProcesses()
                    .Where(p => p.ProcessName.ToLower().Contains("ollama") && 
                           !p.HasExited &&
                           p.StartTime < DateTime.Now.AddMinutes(-1)); // 超过1分钟的进程

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

        public async Task StartDownload(string modelName)
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
            if (string.IsNullOrEmpty(e.Data))
                return;

            var progressArgs = new DownloadProgressEventArgs { Status = "正在下载..." };

            if (e.Data.Contains("pulling manifest"))
            {
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
    }
} 
