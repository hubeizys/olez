using System;

namespace ollez.Services
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public string TimeLeft { get; set; } = string.Empty;
        public long Total { get; set; }
        public long Completed { get; set; }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 