using System;
using System.Threading.Tasks;

namespace ollez.Services
{
    public interface IModelDownloadService
    {
        event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;
        Task StartDownload(string modelName);
        Task StopDownload();
        bool IsDownloading { get; }
        string CurrentModelName { get; }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public string Status { get; set; }
        public string Speed { get; set; }
        public string TimeLeft { get; set; }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}