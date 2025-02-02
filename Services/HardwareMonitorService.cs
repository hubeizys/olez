using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using IODriveInfo = System.IO.DriveInfo;  // 添加别名
using System.Runtime.InteropServices;
using ollez.Models;
using Serilog;

namespace ollez.Services
{
    public class HardwareMonitorService : IHardwareMonitorService
    {
        private readonly ILogger _logger;
        private readonly Timer _updateTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly Process _currentProcess;
        private HardwareInfo _currentInfo;

        public HardwareMonitorService()
        {
            _logger = Log.Logger;
            _updateTimer = new Timer(2000);
            _updateTimer.Elapsed += OnTimerElapsed;
            
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _currentProcess = Process.GetCurrentProcess();
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
                // 先获取一次 CPU 使用率以初始化计数器
                _cpuCounter.NextValue();
                // 等待一小段时间以获取准确的 CPU 使用率
                await Task.Delay(500);

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
                _logger.Error(ex, "Error updating hardware info");
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
                _logger.Error(ex, "Error getting processor name");
            }
            return "Unknown Processor";
        }

        private double GetTotalPhysicalMemory()
        {
            return GetMemoryMetrics().Total;
        }

        private double GetAvailablePhysicalMemory()
        {
            return GetMemoryMetrics().Available;
        }

        private MemoryMetrics GetMemoryMetrics()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var metrics = new MemoryMetrics();
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    metrics.Total = memStatus.ullTotalPhys / (1024.0 * 1024 * 1024);
                    metrics.Available = memStatus.ullAvailPhys / (1024.0 * 1024 * 1024);
                }
                return metrics;
            }
            
            // 对于其他操作系统，可以通过读取 /proc/meminfo 等方式实现
            throw new PlatformNotSupportedException("Currently only Windows is supported");
        }

        private void UpdateDriveInfo(HardwareInfo info)
        {
            info.Drives.Clear();
            foreach (var drive in IODriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                var usagePercentage = ((totalGB - freeGB) / totalGB) * 100;

                info.Drives.Add(new Models.DriveInfo
                {
                    Name = drive.Name,
                    TotalSpace = totalGB,
                    AvailableSpace = freeGB,
                    UsagePercentage = usagePercentage
                });
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private class MemoryMetrics
        {
            public double Total { get; set; }
            public double Available { get; set; }
        }

        public void Dispose()
        {
            _updateTimer?.Dispose();
            _cpuCounter?.Dispose();
        }
    }
}
