using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ollez.Models
{
    public class HardwareInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        private string _cpuName = string.Empty;
        public string CpuName
        {
            get => _cpuName;
            set
            {
                if (_cpuName != value)
                {
                    _cpuName = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _cpuCores;
        public int CpuCores
        {
            get => _cpuCores;
            set
            {
                if (_cpuCores != value)
                {
                    _cpuCores = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _cpuUsage;
        public double CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (_cpuUsage != value)
                {
                    _cpuUsage = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _totalMemory;
        public double TotalMemory
        {
            get => _totalMemory;
            set
            {
                if (_totalMemory != value)
                {
                    _totalMemory = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _availableMemory;
        public double AvailableMemory
        {
            get => _availableMemory;
            set
            {
                if (_availableMemory != value)
                {
                    _availableMemory = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _memoryUsage;
        public double MemoryUsage
        {
            get => _memoryUsage;
            set
            {
                if (_memoryUsage != value)
                {
                    _memoryUsage = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _gpuAvailable;
        public bool GpuAvailable
        {
            get => _gpuAvailable;
            set
            {
                if (_gpuAvailable != value)
                {
                    _gpuAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _gpuName = string.Empty;
        public string GpuName
        {
            get => _gpuName;
            set
            {
                if (_gpuName != value)
                {
                    _gpuName = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _gpuMemoryTotal;
        public int GpuMemoryTotal
        {
            get => _gpuMemoryTotal;
            set
            {
                if (_gpuMemoryTotal != value)
                {
                    _gpuMemoryTotal = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _gpuMemoryUsed;
        public int GpuMemoryUsed
        {
            get => _gpuMemoryUsed;
            set
            {
                if (_gpuMemoryUsed != value)
                {
                    _gpuMemoryUsed = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _gpuMemoryUsage;
        public double GpuMemoryUsage
        {
            get => _gpuMemoryUsage;
            set
            {
                if (_gpuMemoryUsage != value)
                {
                    _gpuMemoryUsage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<DriveInfo> Drives { get; set; } = new ObservableCollection<DriveInfo>();
    }

    public class DriveInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _totalSpace;
        public double TotalSpace
        {
            get => _totalSpace;
            set
            {
                if (_totalSpace != value)
                {
                    _totalSpace = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _availableSpace;
        public double AvailableSpace
        {
            get => _availableSpace;
            set
            {
                if (_availableSpace != value)
                {
                    _availableSpace = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _usagePercentage;
        public double UsagePercentage
        {
            get => _usagePercentage;
            set
            {
                if (_usagePercentage != value)
                {
                    _usagePercentage = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
