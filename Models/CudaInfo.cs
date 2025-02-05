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
        public string DriverVersion { get; set; } = string.Empty;
        public string SmiVersion { get; set; } = string.Empty;
        public GpuInfo[] Gpus { get; set; } = Array.Empty<GpuInfo>();
    }
} 