using System;

namespace ollez.Models
{
    public class GpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public int MemoryTotal { get; set; }
        public int MemoryFree { get; set; }
        public int MemoryUsed { get; set; }
        public string Architecture { get; set; } = string.Empty;
    }

    public class CudaInfo
    {
        public bool IsAvailable { get; set; }
        private string _version = string.Empty;
        public string Version
        {
            get => string.IsNullOrEmpty(_version) ? null : _version;
            set => _version = value;
        }
        private string _driverVersion = string.Empty;
        public string DriverVersion
        {
            get => string.IsNullOrEmpty(_driverVersion) ? null : _driverVersion;
            set => _driverVersion = value;
        }
        public string SmiVersion { get; set; } = string.Empty;
        // public string DriverVersion { get; set; } = string.Empty;
        private bool _hasCudnn = false;
        public bool HasCudnn
        {
            get => _hasCudnn;
            set => _hasCudnn = value;
        }

        private string _cudnnVersion = string.Empty;
        public string CudnnVersion
        {
            get => string.IsNullOrEmpty(_cudnnVersion) ? null : _cudnnVersion;
            set => _cudnnVersion = value;
        }
        public GpuInfo[] Gpus { get; set; } = Array.Empty<GpuInfo>();
    }
} 