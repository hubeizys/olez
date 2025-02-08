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
}