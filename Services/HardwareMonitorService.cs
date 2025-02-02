using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.IO; // 添加这个引用
using ollez.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.Devices; // 添加这个引用

namespace ollez.Services
{
    public class HardwareMonitorService : IHardwareMonitorService
    {
        private readonly ILogger<HardwareMonitorService> _logger;
        private readonly Timer _updateTimer;
        private readonly PerformanceCounter _cpuCounter;
        private HardwareInfo _currentInfo;

        public HardwareMonitorService(ILogger<HardwareMonitorService> logger)
        {
            _logger = logger;
            _updateTimer = new Timer(2000); // 每2秒更新一次
            _updateTimer.Elapsed += OnTimerElapsed;
            
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _currentInfo = new HardwareInfo();
        }

        public void StartMonitoring()
        {
            _updateTimer.Start();
        }

        public void StopMonitoring()
        {
            _updateTimer.Stop();
        }

        public async Task<HardwareInfo> GetHardwareInfoAsync()
        {
            if (_currentInfo == null)
            {
                await UpdateHardwareInfoAsync();
            }
            return _currentInfo;
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await UpdateHardwareInfoAsync();
        }

        private async Task UpdateHardwareInfoAsync()
        {
            try
            {
                _currentInfo = new HardwareInfo
                {
                    CpuName = GetProcessorName(),
                    CpuCores = Environment.ProcessorCount,
                    CpuUsage = _cpuCounter.NextValue(),
                    TotalMemory = GetTotalPhysicalMemory(),
                    AvailableMemory = GetAvailablePhysicalMemory(),
                };

                _currentInfo.MemoryUsage = ((_currentInfo.TotalMemory - _currentInfo.AvailableMemory) / _currentInfo.TotalMemory) * 100;

                UpdateDriveInfo(_currentInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating hardware info");
            }
        }

        private string GetProcessorName()
        {
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\");
                if (key != null)
                {
                    return key.GetValue("ProcessorNameString").ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting processor name");
            }
            return "Unknown Processor";
        }

        private double GetTotalPhysicalMemory()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024 * 1024);
        }

        private double GetAvailablePhysicalMemory()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / (1024.0 * 1024 * 1024);
        }

        private void UpdateDriveInfo(HardwareInfo info)
        {
            info.Drives.Clear();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))  // 使用 System.IO.DriveInfo
            {
                var totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);  // 使用 AvailableFreeSpace 替代 AvailableSize
                var usagePercentage = ((totalGB - freeGB) / totalGB) * 100;

                info.Drives.Add(new Models.DriveInfo  // 明确指定使用 Models.DriveInfo
                {
                    Name = drive.Name,
                    TotalSpace = totalGB,
                    AvailableSpace = freeGB,
                    UsagePercentage = usagePercentage
                });
            }
        }
    }
}
