using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ollez.Services
{
    public class ModelDownloadService : IModelDownloadService
    {
        private Process _process;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDownloading;

        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

        public bool IsDownloading => _isDownloading;

        public async Task StartDownload(string modelName)
        {
            if (_isDownloading)
                return;

            _isDownloading = true;
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
                _process?.Dispose();
                _process = null;
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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"停止下载时出错: {ex.Message}");
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
           
            }
            else if (e.Data.Contains("writing manifest"))
            {
                progressArgs.Status = "正在写入模型文件...";
            }
            else if (e.Data.Contains("verifying sha"))
            {
                progressArgs.Status = "正在验证文件完整性...";
            }


            OnDownloadProgressChanged(progressArgs);
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
