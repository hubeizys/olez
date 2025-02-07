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
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using Serilog;

namespace ollez.ViewModels
{
    public class SystemSetupViewModel : BindableBase
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly ISystemCheckService _systemCheckService;
        private readonly IChatDbService _chatDbService;
        private readonly IModelDownloadService _modelDownloadService;
        private int _currentStep;
        private bool _hasNvidia;
        private string _selectedDrive = string.Empty;
        private ObservableCollection<string> _availableDrives = new();
        private string _selectedInstallPath = @"D:\Ollama";
        private bool _hasOllamaSetup;
        private string _ollamaSetupPath;
        private bool _showGuideIndicator = true;
        private string _userGuide = string.Empty;
        private bool _isOllamaInstalled;
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
        private bool _isEnvControlsEnabled;
        private string _searchQuery;
        private ObservableCollection<OllamaModel> _searchResults;
        private ObservableCollection<DeepseekModel> _deepseekModels;
        private string _commandOutput;
        private bool _showOllamaDownloadButton = true;
        private bool _showNvidiaDownloadButton = true;
        private bool _showCudaDownloadButton = true;
        private string _currentDownloadingModel;
        private bool _isDownloadingOllama;
        private bool _isDownloadingModel;
        private string _ollamaDownloadStatus = "准备下载...";
        private string _modelDownloadStatus = "准备下载...";
        private double _ollamaDownloadProgress;
        private double _modelDownloadProgress;
        private string _nvidiaDownloadStatus;
        private string _cudaDownloadStatus;


        public SystemSetupViewModel(
            IHardwareMonitorService hardwareMonitorService,
            ISystemCheckService systemCheckService,
            IChatDbService chatDbService,
            IModelDownloadService modelDownloadService)
        {
            _hardwareMonitorService = hardwareMonitorService;
            _systemCheckService = systemCheckService;
            _chatDbService = chatDbService;
            _modelDownloadService = modelDownloadService;
            
            // 订阅下载事件
            _modelDownloadService.DownloadProgressChanged += ModelDownloadService_DownloadProgressChanged;
            _modelDownloadService.DownloadCompleted += ModelDownloadService_DownloadCompleted;
            
            // 初始化下载状态
            IsDownloadingModel = _modelDownloadService.IsDownloading;
            if (IsDownloadingModel)
            {
                _currentDownloadingModel = _modelDownloadService.CurrentModelName;
                var modelSize = _currentDownloadingModel?.Split(':').LastOrDefault();
                var targetModel = DeepseekModels?.FirstOrDefault(m => m.Size == modelSize);
                if (targetModel != null)
                {
                    targetModel.IsDownloading = true;
                }
            }
            
            _currentStep = 0;
            
            NextCommand = new DelegateCommand(ExecuteNext, CanExecuteNext);
            PreviousCommand = new DelegateCommand(ExecutePrevious, CanExecutePrevious);
            SkipCommand = new DelegateCommand(ExecuteSkip, CanExecuteSkip);
            SelectInstallPathCommand = new DelegateCommand(ExecuteSelectInstallPath);
            SelectModelPathCommand = new DelegateCommand(ExecuteSelectModelPath);
            InstallOllamaCommand = new DelegateCommand(ExecuteInstallOllama);
            OpenCudaFolderCommand = new DelegateCommand(ExecuteOpenCudaFolder);
            OpenOllamaFolderCommand = new DelegateCommand(ExecuteOpenOllamaFolder);
            DownloadOllamaCommand = new DelegateCommand(async () => await ExecuteDownloadOllama());
            DownloadNvidiaCommand = new DelegateCommand(async () => await ExecuteDownloadNvidia());
            DownloadCudaCommand = new DelegateCommand(async () => await ExecuteDownloadCuda());
            InstallNvidiaCommand = new DelegateCommand(ExecuteInstallNvidia);
            InstallCudaCommand = new DelegateCommand(ExecuteInstallCuda);
            SetupEnvCommand = new DelegateCommand(async () => await ExecuteSetupEnv(), CanExecuteSetupEnv);
            SearchModelsCommand = new DelegateCommand(ExecuteSearchModels);
            InstallModelCommand = new DelegateCommand<string>(ExecuteInstallModel);
            InstallDeepseekModelCommand = new DelegateCommand<string>(ExecuteInstallDeepseekModel);
            
            SearchResults = new ObservableCollection<OllamaModel>();
            DeepseekModels = new ObservableCollection<DeepseekModel>(DeepseekModel.GetDefaultModels());
            
            InitializeAsync();
        }

        private bool CanExecuteSetupEnv()
        {
            return IsSettingEnv && HasLocalNvidiaSetup && HasLocalCudaSetup;
        }

        private async Task ExecuteSetupEnv()
        {
            if (IsSettingEnv) return;

            IsSettingEnv = true;
            EnvSetupProgress = 0;

            try
            {
                // 检查CUDA_PATH环境变量
                var cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH", EnvironmentVariableTarget.Machine);
                if (string.IsNullOrEmpty(cudaPath))
                {
                    NvidiaGuide = "正在设置CUDA环境变量...";
                    EnvSetupProgress = 30;

                    // 查找CUDA安装路径
                    var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    var possibleCudaPaths = Directory.GetDirectories(programFiles, "NVIDIA GPU Computing Toolkit\\CUDA*");
                    
                    if (possibleCudaPaths.Length > 0)
                    {
                        var latestCudaPath = possibleCudaPaths.OrderByDescending(p => p).First();
                        
                        // 使用PowerShell设置环境变量
                        var setEnvCommand = $"[Environment]::SetEnvironmentVariable('CUDA_PATH', '{latestCudaPath}', 'Machine')";
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "powershell",
                            Arguments = $"-Command \"{setEnvCommand}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            Verb = "runas"
                        };

                        using (var process = new Process { StartInfo = startInfo })
                        {
                            process.Start();
                            await process.WaitForExitAsync();
                        }

                        EnvSetupProgress = 60;

                        // 添加到PATH
                        var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
                        var binPath = Path.Combine(latestCudaPath, "bin");
                        if (!path.Contains(binPath))
                        {
                            var setPathCommand = $"[Environment]::SetEnvironmentVariable('PATH', $env:PATH + ';{binPath}', 'Machine')";
                            startInfo.Arguments = $"-Command \"{setPathCommand}\"";
                            
                            using (var process = new Process { StartInfo = startInfo })
                            {
                                process.Start();
                                await process.WaitForExitAsync();
                            }
                        }
                        EnvSetupProgress = 90;
                    }
                }

                EnvSetupProgress = 100;
                NvidiaGuide = "环境变量设置完成！";
                await Task.Delay(1000); // 显示完成状态
            }
            catch (Exception ex)
            {
                NvidiaGuide = $"设置环境变量时出错: {ex.Message}";
                MessageBox.Show($"设置环境变量时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSettingEnv = false;
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
            if (!HasOllamaSetup)
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
                HasOllamaSetup = true;
                OllamaSetupPath = ollamaSetupPath;
                ShowOllamaDownloadButton = false;
            }
            else
            {
                HasOllamaSetup = false;
                ShowOllamaDownloadButton = true;
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

        public bool HasOllamaSetup
        {
            get => _hasOllamaSetup;
            set => SetProperty(ref _hasOllamaSetup, value);
        }

        public string OllamaSetupPath
        {
            get => _ollamaSetupPath;
            set => SetProperty(ref _ollamaSetupPath, value);
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

        public bool IsEnvControlsEnabled
        {
            get => _isEnvControlsEnabled;
            set => SetProperty(ref _isEnvControlsEnabled, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public ObservableCollection<OllamaModel> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        public ObservableCollection<DeepseekModel> DeepseekModels
        {
            get => _deepseekModels;
            set => SetProperty(ref _deepseekModels, value);
        }

        public string CommandOutput
        {
            get => _commandOutput;
            set => SetProperty(ref _commandOutput, value);
        }

        public bool HasSearchResults => SearchResults?.Count > 0;

        public DelegateCommand NextCommand { get; }
        public DelegateCommand PreviousCommand { get; }
        public DelegateCommand SkipCommand { get; }
        public DelegateCommand SelectInstallPathCommand { get; }
        public DelegateCommand SelectModelPathCommand { get; }
        public DelegateCommand InstallOllamaCommand { get; }
        public DelegateCommand OpenCudaFolderCommand { get; }
        public DelegateCommand OpenOllamaFolderCommand { get; }
        public DelegateCommand DownloadOllamaCommand { get; }
        public DelegateCommand DownloadNvidiaCommand { get; }
        public DelegateCommand DownloadCudaCommand { get; }
        public DelegateCommand InstallNvidiaCommand { get; }
        public DelegateCommand InstallCudaCommand { get; }
        public DelegateCommand SetupEnvCommand { get; }
        public DelegateCommand SearchModelsCommand { get; private set; }
        public DelegateCommand<string> InstallModelCommand { get; private set; }
        public DelegateCommand<string> InstallDeepseekModelCommand { get; private set; }

        public bool ShowOllamaDownloadButton
        {
            get => _showOllamaDownloadButton;
            set => SetProperty(ref _showOllamaDownloadButton, value);
        }

        public bool ShowNvidiaDownloadButton
        {
            get => _showNvidiaDownloadButton;
            set => SetProperty(ref _showNvidiaDownloadButton, value);
        }

        public bool ShowCudaDownloadButton
        {
            get => _showCudaDownloadButton;
            set => SetProperty(ref _showCudaDownloadButton, value);
        }

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
            if (string.IsNullOrEmpty(OllamaSetupPath) || !File.Exists(OllamaSetupPath)) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = OllamaSetupPath,
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

        private void ExecuteOpenCudaFolder()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var cudaDir = Path.Combine(appDir, "cuda");
            
            if (Directory.Exists(cudaDir))
            {
                Process.Start("explorer.exe", cudaDir);
            }
            else
            {
                MessageBox.Show("CUDA安装包目录不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteOpenOllamaFolder()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var ollamaDir = Path.Combine(appDir, "ollama");
            
            if (Directory.Exists(ollamaDir))
            {
                Process.Start("explorer.exe", ollamaDir);
            }
            else
            {
                MessageBox.Show("Ollama安装包目录不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task ExecuteDownloadOllama()
        {
            try
            {
                IsDownloadingOllama = true;
                ShowOllamaDownloadButton = false;
                OllamaDownloadStatus = "正在下载Ollama安装包...";
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
                                OllamaDownloadProgress = (double)totalBytesRead / totalBytes;
                                OllamaDownloadStatus = $"下载进度: {(OllamaDownloadProgress * 100):F1}%";
                            }
                        }
                    }
                }

                OllamaDownloadStatus = "下载完成！";
                HasOllamaSetup = true;
                OllamaSetupPath = ollamaSetupPath;
                UpdateUserGuide();
            }
            catch (Exception ex)
            {
                OllamaDownloadStatus = $"下载失败: {ex.Message}";
                ShowOllamaDownloadButton = true;
                UserGuide = "下载失败，请重试";
            }
            finally
            {
                IsDownloadingOllama = false;
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
                NvidiaGuide = "请先安装NVIDIA驱动和CUDA";
                ShowGuideIndicator = true;
                return;
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CUDA_PATH", EnvironmentVariableTarget.Machine)))
            {
                NvidiaGuide = "NVIDIA驱动和CUDA已安装，请点击\"设置环境变量\"按钮完成配置";
                ShowGuideIndicator = true;
                IsEnvControlsEnabled = true;
                return;
            }

            NvidiaGuide = "NVIDIA驱动和CUDA环境已配置完成";
            ShowGuideIndicator = false;
            IsEnvControlsEnabled = false;
        }

        private async Task ExecuteDownloadNvidia()
        {
            if (IsDownloadingNvidia) return;

            try
            {
                IsDownloadingNvidia = true;
                ShowNvidiaDownloadButton = false;
                NvidiaDownloadStatus = "正在下载NVIDIA驱动...";
                NvidiaGuide = "正在下载NVIDIA驱动，请稍候...";

                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var cudaDir = Path.Combine(appDir, "cuda");
                var nvidiaSetupPath = Path.Combine(cudaDir, "NVIDIASetup.exe");

                if (!Directory.Exists(cudaDir))
                {
                    Directory.CreateDirectory(cudaDir);
                }

                using (var client = new HttpClient())
                {
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
                                NvidiaDownloadProgress = (double)totalBytesRead / totalBytes;
                                NvidiaDownloadStatus = $"下载进度: {(NvidiaDownloadProgress * 100):F1}%";
                            }
                        }
                    }
                }

                NvidiaDownloadStatus = "NVIDIA驱动下载完成！";
                _localNvidiaSetupPath = nvidiaSetupPath;
                HasLocalNvidiaSetup = true;
                UpdateNvidiaGuide();
            }
            catch (Exception ex)
            {
                NvidiaDownloadStatus = $"下载失败: {ex.Message}";
                ShowNvidiaDownloadButton = true;
                NvidiaGuide = "NVIDIA驱动下载失败，请重试";
            }
            finally
            {
                IsDownloadingNvidia = false;
            }
        }

        private async Task ExecuteDownloadCuda()
        {
            if (IsDownloadingCuda) return;

            try
            {
                IsDownloadingCuda = true;
                ShowCudaDownloadButton = false;
                CudaDownloadStatus = "正在下载CUDA Toolkit...";
                NvidiaGuide = "正在下载CUDA Toolkit，请稍候...";

                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var cudaDir = Path.Combine(appDir, "cuda");
                var cudaSetupPath = Path.Combine(cudaDir, "CUDASetup.exe");

                if (!Directory.Exists(cudaDir))
                {
                    Directory.CreateDirectory(cudaDir);
                }

                using (var client = new HttpClient())
                {
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
                                CudaDownloadProgress = (double)totalBytesRead / totalBytes;
                                CudaDownloadStatus = $"下载进度: {(CudaDownloadProgress * 100):F1}%";
                            }
                        }
                    }
                }

                CudaDownloadStatus = "CUDA Toolkit下载完成！";
                _localCudaSetupPath = cudaSetupPath;
                HasLocalCudaSetup = true;
                UpdateNvidiaGuide();
            }
            catch (Exception ex)
            {
                CudaDownloadStatus = $"下载失败: {ex.Message}";
                ShowCudaDownloadButton = true;
                NvidiaGuide = "CUDA Toolkit下载失败，请重试";
            }
            finally
            {
                IsDownloadingCuda = false;
            }
        }

        private async Task<string> GetNvidiaDownloadUrl()
        {
            try
            {
                // 获取GPU信息
                var cudaInfo = await _systemCheckService.CheckCudaAsync();
                if (!cudaInfo.IsAvailable || cudaInfo.Gpus.Length == 0)
                {
                    throw new Exception("无法检测到NVIDIA GPU信息");
                }

                // 获取第一个GPU的信息
                var gpuName = cudaInfo.Gpus[0].Name.ToUpper();

                // 检测GPU系列
                string psid = "101"; // 默认值
                string pfid = "815"; // 默认值

                if (gpuName.Contains("RTX 40"))
                {
                    psid = "129"; // RTX 40 Series
                    pfid = "978"; // RTX 4090/4080/4070/4060
                }
                else if (gpuName.Contains("RTX 30"))
                {
                    psid = "127"; // RTX 30 Series
                    pfid = "912"; // RTX 3090/3080/3070/3060
                }
                else if (gpuName.Contains("RTX 20"))
                {
                    psid = "119"; // RTX 20 Series
                    pfid = "921"; // RTX 2080/2070/2060
                }
                else if (gpuName.Contains("GTX 16"))
                {
                    psid = "127"; // GTX 16 Series
                    pfid = "920"; // GTX 1660/1650
                }

                using (var client = new HttpClient())
                {
                    // 构建查询驱动的URL，包含检测到的GPU信息
                    var searchUrl = $"https://www.nvidia.com/Download/processFind.aspx?psid={psid}&pfid={pfid}&osid=57&lid=1&whql=1&lang=en-us&ctk=0&dtcid=1";
                    var response = await client.GetStringAsync(searchUrl);

                    // 解析HTML内容以获取下载链接
                    var startIndex = response.IndexOf("https://us.download.nvidia.com");
                    if (startIndex != -1)
                    {
                        var endIndex = response.IndexOf("\"", startIndex);
                        if (endIndex != -1)
                        {
                            var downloadLink = response.Substring(startIndex, endIndex - startIndex);
                            if (!string.IsNullOrEmpty(downloadLink))
                            {
                                return downloadLink;
                            }
                        }
                    }

                    // 如果无法获取动态链接，使用最新的稳定版驱动链接
                    return "https://us.download.nvidia.com/windows/546.33/546.33-desktop-win10-win11-64bit-international-dch-whql.exe";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取NVIDIA驱动下载链接时出错: {ex.Message}");
                // 使用最新的稳定版驱动作为备用链接
                return "https://us.download.nvidia.com/windows/546.33/546.33-desktop-win10-win11-64bit-international-dch-whql.exe";
            }
        }

        private async Task<string> GetCudaDownloadUrl()
        {
            // CUDA使用固定的下载链接，因为我们需要特定版本
            return "https://developer.download.nvidia.com/compute/cuda/12.3.1/local_installers/cuda_12.3.1_545.23.06_windows.exe";
        }

        private void CheckLocalSetups()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var cudaDir = Path.Combine(appDir, "cuda");
            
            var nvidiaSetupPath = Path.Combine(cudaDir, "NVIDIASetup.exe");
            if (File.Exists(nvidiaSetupPath))
            {
                HasLocalNvidiaSetup = true;
                _localNvidiaSetupPath = nvidiaSetupPath;
                ShowNvidiaDownloadButton = false;
            }
            else
            {
                HasLocalNvidiaSetup = false;
                ShowNvidiaDownloadButton = true;
            }

            var cudaSetupPath = Path.Combine(cudaDir, "CUDASetup.exe");
            if (File.Exists(cudaSetupPath))
            {
                HasLocalCudaSetup = true;
                _localCudaSetupPath = cudaSetupPath;
                ShowCudaDownloadButton = false;
            }
            else
            {
                HasLocalCudaSetup = false;
                ShowCudaDownloadButton = true;
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

            // 初始化时检查已安装的模型
            CheckInstalledModels();
        }

        private async Task SaveOllamaConfigAsync()
        {
            var config = await _chatDbService.GetOllamaConfigAsync() ?? new OllamaConfig();
            config.InstallPath = SelectedInstallPath;
            config.ModelsPath = SelectedModelPath;
            await _chatDbService.SaveOllamaConfigAsync(config);
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

        private async void CheckInstalledModels()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "list",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                foreach (var model in DeepseekModels)
                {
                    model.IsInstalled = output.Contains($"deepseek-r1:{model.Size}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查已安装模型时出错: {ex.Message}");
            }
        }

        private void ExecuteSearchModels()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            try
            {
                // 这里应该调用 Ollama API 搜索模型
                // 目前只是一个示例实现
                SearchResults.Clear();
                SearchResults.Add(new OllamaModel
                {
                    Name = SearchQuery,
                    Description = "搜索到的模型描述"
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"搜索模型时出错: {ex.Message}");
            }
        }

        private async void ExecuteInstallModel(string modelName)
        {
            await InstallModelByTerm(modelName);
        }

        private async void ExecuteInstallDeepseekModel(string size)
        {
            await InstallModelByTerm($"deepseek-r1:{size}");
        }

        private async Task InstallModelByTerm(string modelName)
        {
            var modelSize = modelName.Split(':').LastOrDefault();
            var targetModel = DeepseekModels.FirstOrDefault(m => m.Size == modelSize);
            try
            {
                // 如果当前正在下载同一个模型，直接返回
                if (_modelDownloadService.IsDownloading && _modelDownloadService.CurrentModelName == modelName)
                {
                    return;
                }

                IsDownloadingModel = true;
                ModelDownloadStatus = "正在初始化下载环境...";
                CommandOutput = "正在初始化下载环境...";
                ModelDownloadProgress = 0;
                _currentDownloadingModel = modelName;

                if (targetModel != null)
                {
                    targetModel.IsDownloading = true;
                    targetModel.DownloadProgress = 0;
                }

                await _modelDownloadService.StartDownload(modelName);
            }
            catch (Exception ex)
            {
                ModelDownloadStatus = $"启动下载失败: {ex.Message}";
                Debug.WriteLine($"启动下载时出错: {ex.Message}");
                CommandOutput = $"发生错误: {ex.Message}";
                IsDownloadingModel = false;
                if (targetModel != null)
                {
                    targetModel.IsDownloading = false;
                }
                _currentDownloadingModel = null;
            }
        }

        private void ModelDownloadService_DownloadProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ModelDownloadProgress = e.Progress;
                ModelDownloadStatus = e.Status;
                CommandOutput = e.Status;
                
                var modelSize = _currentDownloadingModel?.Split(':').LastOrDefault();
                var targetModel = DeepseekModels.FirstOrDefault(m => m.Size == modelSize);
                if (targetModel != null)
                {
                    targetModel.DownloadProgress = e.Progress;
                }
            });
        }

        private void ModelDownloadService_DownloadCompleted(object sender, DownloadCompletedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ModelDownloadStatus = e.Message;
                IsDownloadingModel = false;
                
                var modelSize = _currentDownloadingModel?.Split(':').LastOrDefault();
                var targetModel = DeepseekModels.FirstOrDefault(m => m.Size == modelSize);
                if (targetModel != null)
                {
                    targetModel.IsDownloading = false;
                }
                
                if (e.Success)
                {
                    CheckInstalledModels();
                }
                
                _currentDownloadingModel = null;
            });
        }

        public bool IsDownloadingOllama
        {
            get => _isDownloadingOllama;
            set => SetProperty(ref _isDownloadingOllama, value);
        }

        public bool IsDownloadingNvidia
        {
            get => _isDownloadingNvidia;
            set => SetProperty(ref _isDownloadingNvidia, value);
        }

        public bool IsDownloadingCuda
        {
            get => _isDownloadingCuda;
            set => SetProperty(ref _isDownloadingCuda, value);
        }

        public bool IsDownloadingModel
        {
            get => _isDownloadingModel;
            set => SetProperty(ref _isDownloadingModel, value);
        }

        public string OllamaDownloadStatus
        {
            get => _ollamaDownloadStatus;
            set => SetProperty(ref _ollamaDownloadStatus, value);
        }

        public string NvidiaDownloadStatus
        {
            get => _nvidiaDownloadStatus;
            set => SetProperty(ref _nvidiaDownloadStatus, value);
        }

        public string CudaDownloadStatus
        {
            get => _cudaDownloadStatus;
            set => SetProperty(ref _cudaDownloadStatus, value);
        }

        public string ModelDownloadStatus
        {
            get => _modelDownloadStatus;
            set => SetProperty(ref _modelDownloadStatus, value);
        }

        public double OllamaDownloadProgress
        {
            get => _ollamaDownloadProgress;
            set => SetProperty(ref _ollamaDownloadProgress, value);
        }

        public double NvidiaDownloadProgress
        {
            get => _nvidiaDownloadProgress;
            set => SetProperty(ref _nvidiaDownloadProgress, value);
        }

        public double CudaDownloadProgress
        {
            get => _cudaDownloadProgress;
            set => SetProperty(ref _cudaDownloadProgress, value);
        }

        public double ModelDownloadProgress
        {
            get => _modelDownloadProgress;
            set => SetProperty(ref _modelDownloadProgress, value);
        }
    }
} 
