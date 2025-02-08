using System;
using Prism.Mvvm;

namespace ollez.Models
{
    public class DeepseekModel : BindableBase
    {
        private string _size = string.Empty;
        private double _requiredVRAM;
        private bool _isInstalled;
        private double _downloadProgress;
        private bool _isDownloading;

        public string Size
        {

            get => _size;
            set => SetProperty(ref _size, value);
        }

        public double RequiredVRAM
        {

            get => _requiredVRAM;
            set => SetProperty(ref _requiredVRAM, value);
        }

        public bool IsInstalled
        {

            get => _isInstalled;
            set => SetProperty(ref _isInstalled, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public static DeepseekModel[] GetDefaultModels()
        {
            return new[]
            {
                new DeepseekModel { Size = "1.5b", RequiredVRAM = 2 },
                new DeepseekModel { Size = "7b", RequiredVRAM = 8 },
                new DeepseekModel { Size = "8b", RequiredVRAM = 8 },
                new DeepseekModel { Size = "14b", RequiredVRAM = 14 },
                new DeepseekModel { Size = "32b", RequiredVRAM = 32 },
                new DeepseekModel { Size = "70b", RequiredVRAM = 70 },
                new DeepseekModel { Size = "671b", RequiredVRAM = 671 }
            };
        }
    }
}