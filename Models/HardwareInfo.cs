using System.Collections.ObjectModel;

namespace ollez.Models
{
    public class HardwareInfo
    {
        public string CpuName { get; set; }
        public int CpuCores { get; set; }
        public double CpuUsage { get; set; }
        public double TotalMemory { get; set; }
        public double AvailableMemory { get; set; }
        public double MemoryUsage { get; set; }
        public ObservableCollection<DriveInfo> Drives { get; set; } = new ObservableCollection<DriveInfo>();
    }

    public class DriveInfo
    {
        public string Name { get; set; }
        public double TotalSpace { get; set; }
        public double AvailableSpace { get; set; }
        public double UsagePercentage { get; set; }
    }
}
