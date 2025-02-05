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
        private readonly IChatDbService _chatDbService;
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
        private CudaInfo _cudaInfo = new();
        private string _nvidiaGuide = string.Empty;
        private double _envSetupProgress;
        private bool _isSettingEnv;
        private bool _hasLocalNvidiaSetup;
        private bool _hasLocalCudaSetup;
        private string _localNvidiaSetupPath;
        private string _localCudaSetupPath;
        private bool _isDownloadingNvidia;
        private bool _isDownloadingCuda;
        private double _nvidiaDownloadProgress;
        private double _cudaDownloadProgress;

        public SystemSetupViewModel(
            IHardwareMonitorService hardwareMonitorService,
            ISystemCheckService systemCheckService,
            IChatDbService chatDbService)
        {
            _hardwareMonitorService = hardwareMonitorService;
            _systemCheckService = systemCheckService;
            _chatDbService = chatDbService;
            _currentStep = 0;
            
            NextCommand = new DelegateCommand(ExecuteNext, CanExecuteNext);
            PreviousCommand = new DelegateCommand(ExecutePrevious, CanExecutePrevious);
            SkipCommand = new DelegateCommand(ExecuteSkip, CanExecuteSkip);
            SelectInstallPathCommand = new DelegateCommand(ExecuteSelectInstallPath);
            SelectModelPathCommand = new DelegateCommand(ExecuteSelectModelPath);
            InstallOllamaCommand = new DelegateCommand(ExecuteInstallOllama);
            OpenLocalSetupFolderCommand = new DelegateCommand(ExecuteOpenLocalSetupFolder);
            DownloadOllamaCommand = new DelegateCommand(async () => await ExecuteDownloadOllama());
            DownloadNvidiaCommand = new DelegateCommand(async () => await ExecuteDownloadNvidia());
            DownloadCudaCommand = new DelegateCommand(async () => await ExecuteDownloadCuda());
            InstallNvidiaCommand = new DelegateCommand(ExecuteInstallNvidia);
            InstallCudaCommand = new DelegateCommand(ExecuteInstallCuda);
            SetupEnvCommand = new DelegateCommand(async () => await ExecuteSetupEnv(), CanExecuteSetupEnv);
            
            InitializeAsync();
        }

        private bool CanExecuteSetupEnv()
        {
            return IsSettingEnv && HasLocalNvidiaSetup && HasLocalCudaSetup;
        }

        private async Task ExecuteSetupEnv()
        {
            try
            {
                IsSettingEnv = false;
                await Task.Delay(1000);
                IsSettingEnv = true;
                EnvSetupProgress = 0;
                EnvSetupProgress = 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置环境变量时出错: {ex.Message}");
                MessageBox.Show($"设置环境变量时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);   
            }
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
            set
            {
                if (SetProperty(ref _selectedInstallPath, value))
                {
                    SaveOllamaConfigAsync().ConfigureAwait(false);
                }
            }
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
            set
            {
                if (SetProperty(ref _selectedModelPath, value))
                {
                    SaveOllamaConfigAsync().ConfigureAwait(false);
                }
            }
        }

        public CudaInfo CudaInfo
        {
            get => _cudaInfo;
            set => SetProperty(ref _cudaInfo, value);
        }

        public string NvidiaGuide
        {
            get => _nvidiaGuide;
            set => SetProperty(ref _nvidiaGuide, value);
        }

        public double EnvSetupProgress
        {
            get => _envSetupProgress;
            set => SetProperty(ref _envSetupProgress, value);
        }

        public bool IsSettingEnv
        {
            get => _isSettingEnv;
            set
            {
                if (SetProperty(ref _isSettingEnv, value))
                {
                    ((DelegateCommand)SetupEnvCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasLocalNvidiaSetup
        {
            get => _hasLocalNvidiaSetup;
            set => SetProperty(ref _hasLocalNvidiaSetup, value);
        }

        public bool HasLocalCudaSetup
        {
            get => _hasLocalCudaSetup;
            set => SetProperty(ref _hasLocalCudaSetup, value);
        }

        public DelegateCommand NextCommand { get; }
        public DelegateCommand PreviousCommand { get; }
        public DelegateCommand SkipCommand { get; }
        public DelegateCommand SelectInstallPathCommand { get; }
        public DelegateCommand SelectModelPathCommand { get; }
        public DelegateCommand InstallOllamaCommand { get; }
        public DelegateCommand OpenLocalSetupFolderCommand { get; }
        public DelegateCommand DownloadOllamaCommand { get; }
        public DelegateCommand DownloadNvidiaCommand { get; }
        public DelegateCommand DownloadCudaCommand { get; }
        public DelegateCommand InstallNvidiaCommand { get; }
        public DelegateCommand InstallCudaCommand { get; }
        public DelegateCommand SetupEnvCommand { get; }

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

        private async Task CheckNvidiaAndCuda()
        {
            try
            {
                CudaInfo = await _systemCheckService.CheckCudaAsync();
                UpdateNvidiaGuide();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查NVIDIA和CUDA状态时出错: {ex.Message}");
            }
        }

        private void UpdateNvidiaGuide()
        {
            if (!CudaInfo.IsAvailable)
            {
                NvidiaGuide = "请先安装NVIDIA驱动和CUDA Toolkit，这将帮助提高AI模型的性能。";
                ShowGuideIndicator = true;
                return;
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CUDA_PATH", EnvironmentVariableTarget.Machine)))
            {
                NvidiaGuide = "NVIDIA驱动和CUDA已安装，请点击“设置环境变量”按钮完成配置。";
                ShowGuideIndicator = true;
                return;
            }

            NvidiaGuide = "NVIDIA驱动和CUDA环境配置已完成！";
            ShowGuideIndicator = false;
        }

        private async Task ExecuteDownloadNvidia()
        {
            if (_isDownloadingNvidia) return;

            try
            {
                _isDownloadingNvidia = true;
                ShowDownloadButton = false;
                DownloadStatus = "正在下载NVIDIA驱动...";
                NvidiaGuide = "正在下载NVIDIA驱动，请稍候...";

                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var setupDir = Path.Combine(appDir, "setup");
                var nvidiaSetupPath = Path.Combine(setupDir, "NVIDIASetup.exe");

                if (!Directory.Exists(setupDir))
                {
                    Directory.CreateDirectory(setupDir);
                }

                using (var client = new HttpClient())
                {
                    // 这里需要根据实际情况获取下载链接
                    var downloadUrl = await GetNvidiaDownloadUrl();
                    var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(nvidiaSetupPath, FileMode.Create, FileAccess.Write, FileShare.None))
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
                                _nvidiaDownloadProgress = (double)totalBytesRead / totalBytes;
                                DownloadProgress = _nvidiaDownloadProgress;
                                DownloadStatus = $"下载进度: {(_nvidiaDownloadProgress * 100):F1}%";
                            }
                        }
                    }
                }

                DownloadStatus = "NVIDIA驱动下载完成！";
                _localNvidiaSetupPath = nvidiaSetupPath;
                HasLocalNvidiaSetup = true;
                UpdateNvidiaGuide();
            }
            catch (Exception ex)
            {
                DownloadStatus = $"下载失败: {ex.Message}";
                ShowDownloadButton = true;
                NvidiaGuide = "NVIDIA驱动下载失败，请重试";
            }
            finally
            {
                _isDownloadingNvidia = false;
            }
        }

        private async Task ExecuteDownloadCuda()
        {
            if (_isDownloadingCuda) return;

            try
            {
                _isDownloadingCuda = true;
                ShowDownloadButton = false;
                DownloadStatus = "正在下载CUDA Toolkit...";
                NvidiaGuide = "正在下载CUDA Toolkit，请稍候...";

                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var setupDir = Path.Combine(appDir, "setup");
                var cudaSetupPath = Path.Combine(setupDir, "CUDASetup.exe");

                if (!Directory.Exists(setupDir))
                {
                    Directory.CreateDirectory(setupDir);
                }

                using (var client = new HttpClient())
                {
                    // 这里需要根据实际情况获取下载链接
                    var downloadUrl = await GetCudaDownloadUrl();
                    var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(cudaSetupPath, FileMode.Create, FileAccess.Write, FileShare.None))
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
                                _cudaDownloadProgress = (double)totalBytesRead / totalBytes;
                                DownloadProgress = _cudaDownloadProgress;
                                DownloadStatus = $"下载进度: {(_cudaDownloadProgress * 100):F1}%";
                            }
                        }
                    }
                }

                DownloadStatus = "CUDA Toolkit下载完成！";
                _localCudaSetupPath = cudaSetupPath;
                HasLocalCudaSetup = true;
                UpdateNvidiaGuide();
            }
            catch (Exception ex)
            {
                DownloadStatus = $"下载失败: {ex.Message}";
                ShowDownloadButton = true;
                NvidiaGuide = "CUDA Toolkit下载失败，请重试";
            }
            finally
            {
                _isDownloadingCuda = false;
            }
        }

        private void ExecuteInstallNvidia()
        {
            if (string.IsNullOrEmpty(_localNvidiaSetupPath) || !File.Exists(_localNvidiaSetupPath)) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = _localNvidiaSetupPath,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                NvidiaGuide = "正在安装NVIDIA驱动，请按照安装向导完成安装...";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安装NVIDIA驱动时出错: {ex.Message}");
                MessageBox.Show($"安装NVIDIA驱动时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                NvidiaGuide = "安装失败，请重试";
            }
        }

        private void ExecuteInstallCuda()
        {
            if (string.IsNullOrEmpty(_localCudaSetupPath) || !File.Exists(_localCudaSetupPath)) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = _localCudaSetupPath,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                NvidiaGuide = "正在安装CUDA Toolkit，请按照安装向导完成安装...";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安装CUDA Toolkit时出错: {ex.Message}");
                MessageBox.Show($"安装CUDA Toolkit时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                NvidiaGuide = "安装失败，请重试";
            }
        }

        private async Task<string> GetNvidiaDownloadUrl()
        {
            // 这里需要实现获取NVIDIA驱动下载链接的逻辑
            // 可能需要调用NVIDIA API或解析网页
            throw new NotImplementedException("需要实现获取NVIDIA驱动下载链接的逻辑");
        }

        private async Task<string> GetCudaDownloadUrl()
        {
            // 这里需要实现获取CUDA Toolkit下载链接的逻辑
            // 可能需要调用NVIDIA API或解析网页
            throw new NotImplementedException("需要实现获取CUDA Toolkit下载链接的逻辑");
        }

        private void CheckLocalSetups()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var setupDir = Path.Combine(appDir, "setup");
            
            var nvidiaSetupPath = Path.Combine(setupDir, "NVIDIASetup.exe");
            if (File.Exists(nvidiaSetupPath))
            {
                HasLocalNvidiaSetup = true;
                _localNvidiaSetupPath = nvidiaSetupPath;
            }

            var cudaSetupPath = Path.Combine(setupDir, "CUDASetup.exe");
            if (File.Exists(cudaSetupPath))
            {
                HasLocalCudaSetup = true;
                _localCudaSetupPath = cudaSetupPath;
            }
        }

        private async void InitializeAsync()
        {
            InitializeDrives();
            CheckLocalSetup();
            CheckLocalSetups();
            UpdateUserGuide();
            StartOllamaInstallationCheck();
            await CheckNvidiaAndCuda();

            // 从数据库加载OllamaConfig
            var config = await _chatDbService.GetOllamaConfigAsync();
            if (config != null)
            {
                _selectedModelPath = config.ModelsPath;
                _selectedInstallPath = config.InstallPath;
                RaisePropertyChanged(nameof(SelectedModelPath));
                RaisePropertyChanged(nameof(SelectedInstallPath));
            }
        }

        private async Task SaveOllamaConfigAsync()
        {
            var config = await _chatDbService.GetOllamaConfigAsync() ?? new OllamaConfig();
            config.InstallPath = SelectedInstallPath;
            config.ModelsPath = SelectedModelPath;
            await _chatDbService.SaveOllamaConfigAsync(config);
        }
    }
} 
