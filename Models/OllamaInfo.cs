using System;
using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace ollez.Models
{
    public class OllamaInfo : BindableBase
    {
        private bool _isRunning;
        private string _version = string.Empty;
        private string _buildType = string.Empty;
        private string _endpoint = string.Empty;
        private string _installPath = string.Empty;
        private string _modelsPath = string.Empty;
        private string _error = string.Empty;
        private bool _hasError;
        private ObservableCollection<OllamaModel> _installedModels = new();
        private string _status = string.Empty;
        private string _commitHash = string.Empty;

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string BuildType
        {
            get => _buildType;
            set => SetProperty(ref _buildType, value);
        }

        public string Endpoint
        {
            get => _endpoint;
            set => SetProperty(ref _endpoint, value);
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

        public string Error
        {
            get => _error;
            set
            {
                SetProperty(ref _error, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public ObservableCollection<OllamaModel> InstalledModels
        {
            get => _installedModels;
            set => SetProperty(ref _installedModels, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string CommitHash
        {
            get => _commitHash;
            set => SetProperty(ref _commitHash, value);
        }
    }

    public class OllamaModel : BindableBase
    {
        private string _name = string.Empty;
        private string _digest = string.Empty;
        private double _size;
        private bool _isRunning;
        private string _status = string.Empty;
        private string _modelPath = string.Empty;
        private string _modifiedAt = string.Empty;
        private bool _hasError;
        private string _error = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Digest
        {
            get => _digest;
            set => SetProperty(ref _digest, value);
        }

        public double Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string ModelPath
        {
            get => _modelPath;
            set => SetProperty(ref _modelPath, value);
        }

        public string ModifiedAt
        {
            get => _modifiedAt;
            set => SetProperty(ref _modifiedAt, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string Error
        {
            get => _error;
            set
            {
                SetProperty(ref _error, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }
    }
} 