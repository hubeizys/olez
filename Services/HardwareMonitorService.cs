using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using IODriveInfo = System.IO.DriveInfo;  // 添加别名
using System.Runtime.InteropServices;
using ollez.Models;
using Serilog;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using DriveInfo = ollez.Models.DriveInfo;

namespace ollez.Services
{
    public class HardwareMonitorService : IHardwareMonitorService, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly ILogger _logger;
        private readonly Timer _updateTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly Process _currentProcess;
        private readonly ISystemCheckService _systemCheckService;
        private HardwareInfo _currentInfo = new();

        public HardwareMonitorService(ISystemCheckService systemCheckService)
        {
            _logger = Log.Logger;
            _updateTimer = new Timer(1000);
            _updateTimer.Elapsed += OnTimerElapsed;

            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _currentProcess = Process.GetCurrentProcess();
            _systemCheckService = systemCheckService;

            // 初始化硬件信息
            _currentInfo = new HardwareInfo
            {
                CpuName = GetProcessorName(),
                CpuCores = Environment.ProcessorCount,
                TotalMemory = GetTotalPhysicalMemory(),
                AvailableMemory = GetAvailablePhysicalMemory(),
                Drives = new System.Collections.ObjectModel.ObservableCollection<Models.DriveInfo>()
            };

            // 初始化时立即更新一次硬盘信息和GPU信息
            UpdateDriveInfo(_currentInfo);
            _ = UpdateGpuInfoAsync();
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

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            await UpdateHardwareInfoAsync();
        }

        private async Task UpdateHardwareInfoAsync()
        {
            try
            {
                // 获取 CPU 使用率
                var cpuUsage = _cpuCounter.NextValue();

                // 获取内存信息
                var memoryMetrics = GetMemoryMetrics();

                // 获取GPU信息
                await UpdateGpuInfoAsync();

                // 在UI线程上更新数据
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _currentInfo.CpuUsage = cpuUsage;
                    _currentInfo.TotalMemory = memoryMetrics.Total;
                    _currentInfo.AvailableMemory = memoryMetrics.Available;
                    _currentInfo.MemoryUsage = ((memoryMetrics.Total - memoryMetrics.Available) / memoryMetrics.Total) * 100;

                    // 更新硬盘信息
                    UpdateDriveInfo(_currentInfo);
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating hardware info");
            }
        }

        private async Task UpdateGpuInfoAsync()
        {
            try
            {
                var cudaInfo = await _systemCheckService.CheckCudaAsync();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _currentInfo.GpuAvailable = cudaInfo.IsAvailable;
                    if (cudaInfo.IsAvailable && cudaInfo.Gpus.Length > 0)
                    {
                        var gpu = cudaInfo.Gpus[0]; // 使用第一个GPU的信息
                        _currentInfo.GpuName = gpu.Name;
                        _currentInfo.GpuMemoryTotal = gpu.MemoryTotal;
                        _currentInfo.GpuMemoryUsed = gpu.MemoryUsed;
                        _currentInfo.GpuMemoryUsage = gpu.MemoryTotal > 0
                            ? (gpu.MemoryUsed / (double)gpu.MemoryTotal) * 100
                            : 0;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating GPU info");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _currentInfo.GpuAvailable = false;
                });
            }
        }

        private string GetProcessorName()
        {
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\");
                if (key != null)
                {
                    var value = key.GetValue("ProcessorNameString");
                    return value?.ToString() ?? "Unknown Processor";
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
            // 获取当前系统中的硬盘信息
            var currentDrives = IODriveInfo.GetDrives().Where(d => d.IsReady).ToList();

            // 遍历当前集合中的硬盘信息
            foreach (var existingDrive in info.Drives.ToList())
            {
                // 如果当前硬盘不在系统中，则从集合中移除
                if (!currentDrives.Any(d => d.Name == existingDrive.Name))
                {
                    info.Drives.Remove(existingDrive);
                }
            }

            // 遍历系统中的硬盘信息
            foreach (var drive in currentDrives)
            {
                var totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                var usagePercentage = ((totalGB - freeGB) / totalGB) * 100;

                // 查找集合中是否已存在该硬盘
                var existingDrive = info.Drives.FirstOrDefault(d => d.Name == drive.Name);

                if (existingDrive != null)
                {
                    // 如果存在，则更新属性
                    existingDrive.TotalSpace = totalGB;
                    existingDrive.AvailableSpace = freeGB;
                    existingDrive.UsagePercentage = usagePercentage;
                }
                else
                {
                    // 如果不存在，则添加到集合中
                    info.Drives.Add(new DriveInfo
                    {
                        Name = drive.Name,
                        TotalSpace = totalGB,
                        AvailableSpace = freeGB,
                        UsagePercentage = usagePercentage
                    });
                }
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
