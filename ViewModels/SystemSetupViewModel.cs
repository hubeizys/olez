using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using ollez.Models;
using ollez.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Humanizer;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace ollez.ViewModels
{
    public class SystemSetupViewModel : BindableBase
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly ISystemCheckService _systemCheckService;
        private int _currentStep;
        private bool _hasNvidia;
        private string _selectedDrive = string.Empty;
        private ObservableCollection<string> _availableDrives = new();
        private string _selectedInstallPath = @"D:\Ollama";
        private bool _hasLocalSetup;
        private string _localSetupPath;
        private string _link = "https://ollama.com/download";
        private bool _isDownloading;
        private double _downloadProgress;
        private string _downloadStatus = "准备下载...";
        private bool _showDownloadButton = true;
        private string _userGuide = string.Empty;
        private bool _isOllamaInstalled;
        private bool _showGuideIndicator = true;
        private CancellationTokenSource? _installCheckCts;
        private string _selectedModelPath = string.Empty;

        public SystemSetupViewModel(IHardwareMonitorService hardwareMonitorService, ISystemCheckService systemCheckService)
        {
            _hardwareMonitorService = hardwareMonitorService;
            _systemCheckService = systemCheckService;
            _currentStep = 0;
            
            NextCommand = new DelegateCommand(ExecuteNext, CanExecuteNext);
            PreviousCommand = new DelegateCommand(ExecutePrevious, CanExecutePrevious);
            SkipCommand = new DelegateCommand(ExecuteSkip, CanExecuteSkip);
            SelectInstallPathCommand = new DelegateCommand(ExecuteSelectInstallPath);
            SelectModelPathCommand = new DelegateCommand(ExecuteSelectModelPath);
            InstallOllamaCommand = new DelegateCommand(ExecuteInstallOllama);
            OpenLocalSetupFolderCommand = new DelegateCommand(ExecuteOpenLocalSetupFolder);
            DownloadOllamaCommand = new DelegateCommand(async () => await ExecuteDownloadOllama());
            
            InitializeDrives();
            CheckLocalSetup();
            UpdateUserGuide();
            StartOllamaInstallationCheck();
        }

        private void StartOllamaInstallationCheck()
        {
            _installCheckCts?.Cancel();
            _installCheckCts = new CancellationTokenSource();
            
            Task.Run(async () =>
            {
                while (!_installCheckCts.Token.IsCancellationRequested)
                {
                    var ollamaInfo = await _systemCheckService.CheckOllamaAsync();
                    IsOllamaInstalled = ollamaInfo.IsRunning;
                    await Task.Delay(2000, _installCheckCts.Token);
                }
            }, _installCheckCts.Token);
        }

        private void UpdateUserGuide()
        {
            if (!HasLocalSetup)
            {
                UserGuide = "请先下载Ollama安装包";
                ShowGuideIndicator = true;
                return;
            }

            if (!IsOllamaInstalled)
            {
                UserGuide = "请点击\"安装\"按钮安装Ollama";
                ShowGuideIndicator = true;
                return;
            }

            UserGuide = "安装完成，请点击\"下一步\"继续";
            ShowGuideIndicator = false;
        }

        public string UserGuide
        {
            get => _userGuide;
            set => SetProperty(ref _userGuide, value);
        }

        public bool IsOllamaInstalled
        {
            get => _isOllamaInstalled;
            set
            {
                if (SetProperty(ref _isOllamaInstalled, value))
                {
                    UpdateUserGuide();
                    ((DelegateCommand)NextCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool ShowGuideIndicator
        {
            get => _showGuideIndicator;
            set => SetProperty(ref _showGuideIndicator, value);
        }

        private void CheckLocalSetup()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var ollamaSetupPath = Path.Combine(appDir, "ollama", "OllamaSetup.exe");
            if (File.Exists(ollamaSetupPath))
            {
                HasLocalSetup = true;
                LocalSetupPath = ollamaSetupPath;
                ShowDownloadButton = false;
            }
            else
            {
                HasLocalSetup = false;
                ShowDownloadButton = true;
            }
        }

        private async void InitializeDrives()
        {
            var hardwareInfo = await _hardwareMonitorService.GetHardwareInfoAsync();
            foreach (var drive in hardwareInfo.Drives.Where(d => d.AvailableSpace > 20))
            {
                AvailableDrives.Add($"{drive.Name} (可用空间: {drive.AvailableSpace:F1} GB)");
            }
            
            // 默认选择第一个非C盘且空间足够的驱动器
            SelectedDrive = AvailableDrives.FirstOrDefault(d => !d.StartsWith("C:")) ?? AvailableDrives.FirstOrDefault() ?? string.Empty;
        }

        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        public bool HasNvidia
        {
            get => _hasNvidia;
            set => SetProperty(ref _hasNvidia, value);
        }

        public string SelectedDrive
        {
            get => _selectedDrive;
            set => SetProperty(ref _selectedDrive, value);
        }

        public ObservableCollection<string> AvailableDrives
        {
            get => _availableDrives;
            set => SetProperty(ref _availableDrives, value);
        }

        public string SelectedInstallPath
        {
            get => _selectedInstallPath;
            set => SetProperty(ref _selectedInstallPath, value);
        }

        public bool HasLocalSetup
        {
            get => _hasLocalSetup;
            set => SetProperty(ref _hasLocalSetup, value);
        }

        public string LocalSetupPath
        {
            get => _localSetupPath;
            set => SetProperty(ref _localSetupPath, value);
        }

        public string Link
        {
            get => _link;
            set => SetProperty(ref _link, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public string DownloadStatus
        {
            get => _downloadStatus;
            set => SetProperty(ref _downloadStatus, value);
        }

        public bool ShowDownloadButton
        {
            get => _showDownloadButton;
            set => SetProperty(ref _showDownloadButton, value);
        }

        public string SelectedModelPath
        {
            get => _selectedModelPath;
            set => SetProperty(ref _selectedModelPath, value);
        }

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand SkipCommand { get; }
        public DelegateCommand SelectInstallPathCommand { get; }
        public DelegateCommand SelectModelPathCommand { get; }
        public DelegateCommand InstallOllamaCommand { get; }
        public DelegateCommand OpenLocalSetupFolderCommand { get; }
        public DelegateCommand DownloadOllamaCommand { get; }

        private void ExecuteNext()
        {
            if (CurrentStep < 2)
            {
                CurrentStep++;
            }
        }

        private bool CanExecuteNext()
        {
            if (CurrentStep == 1 && !IsOllamaInstalled)
            {
                return false;
            }
            return CurrentStep < 2;
        }

        private void ExecutePrevious()
        {
            if (CurrentStep > 0)
            {
                CurrentStep--;
            }
        }

        private bool CanExecutePrevious()
        {
            return CurrentStep > 0;
        }

        private void ExecuteSkip()
        {
            if (CurrentStep == 0) // 只有第一步（显卡驱动）可以跳过
            {
                CurrentStep++;
            }
        }

        private bool CanExecuteSkip()
        {
            return CurrentStep == 0;
        }

        private void ExecuteSelectInstallPath()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "选择Ollama安装路径",
                FileName = "Ollama",
                InitialDirectory = Path.GetDirectoryName(SelectedInstallPath),
                Filter = "文件夹|*.this.directory",
                CheckFileExists = false,
                CheckPathExists = true,
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedInstallPath = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void ExecuteSelectModelPath()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "选择模型存储路径",
                FileName = "models", // 默认文件夹名
                Filter = "文件夹|*.this.directory",
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    SelectedModelPath = folderPath;
                    UpdateUserGuide();
                }
            }
        }

        private void ExecuteInstallOllama()
        {
            if (string.IsNullOrEmpty(SelectedInstallPath)) return;
            if (string.IsNullOrEmpty(LocalSetupPath) || !File.Exists(LocalSetupPath)) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = LocalSetupPath,
                Arguments = $"/DIR=\"{SelectedInstallPath}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                UserGuide = "正在安装Ollama，请等待安装完成...";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安装Ollama时出错: {ex.Message}");
                MessageBox.Show($"安装Ollama时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UserGuide = "安装失败，请重试";
            }
        }

        private void ExecuteOpenLocalSetupFolder()
        {
            if (!string.IsNullOrEmpty(LocalSetupPath) && File.Exists(LocalSetupPath))
            {
                Process.Start("explorer.exe", $"/select,\"{LocalSetupPath}\"");
            }
        }

        private async Task ExecuteDownloadOllama()
        {
            try
            {
                IsDownloading = true;
                ShowDownloadButton = false;
                DownloadStatus = "正在下载Ollama安装包...";
                UserGuide = "正在下载安装包，请稍候...";

                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var ollamaDir = Path.Combine(appDir, "ollama");
                var ollamaSetupPath = Path.Combine(ollamaDir, "OllamaSetup.exe");

                if (!Directory.Exists(ollamaDir))
                {
                    Directory.CreateDirectory(ollamaDir);
                }

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://ollama.com/download/OllamaSetup.exe", HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(ollamaSetupPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        var totalBytesRead = 0L;
                        var bytesRead = 0;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (totalBytes != -1)
                            {
                                DownloadProgress = (double)totalBytesRead / totalBytes;
                                DownloadStatus = $"下载进度: {(DownloadProgress * 100):F1}%";
                            }
                        }
                    }
                }

                DownloadStatus = "下载完成！";
                HasLocalSetup = true;
                LocalSetupPath = ollamaSetupPath;
                UpdateUserGuide();
            }
            catch (Exception ex)
            {
                DownloadStatus = $"下载失败: {ex.Message}";
                ShowDownloadButton = true;
                UserGuide = "下载失败，请重试";
            }
            finally
            {
                IsDownloading = false;
            }
        }
    }
} 
