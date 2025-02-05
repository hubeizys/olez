using System;
using Prism.Mvvm;
namespace ollez.Models
{
    public class OllamaConfig : BindableBase
    {
        public int Id { get; set; }
        private string _installPath = string.Empty;
        private string _modelsPath = string.Empty;
        private DateTime _lastUpdated = DateTime.Now;

        public OllamaConfig()
        {
            LastUpdated = DateTime.Now;
            // 默认路径来自数据库 
        }

        public string InstallPath
        {
            get => _installPath;
            set => SetProperty(ref _installPath, value);
        }

        public string ModelsPath
        {
            get => _modelsPath;
            set => SetProperty(ref _modelsPath, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

    }
} 