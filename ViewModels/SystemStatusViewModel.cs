/*
 * 文件名：SystemStatusViewModel.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：系统状态视图的视图模型
 */

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using ollez.Data;
using ollez.Models;
using ollez.Services;
using ollez.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace ollez.ViewModels
{
    /// <summary>
    /// 系统状态视图的视图模型
    /// </summary>
    public class SystemStatusViewModel : BindableBase, INavigationAware
    {
        private readonly ISystemCheckService _systemCheckService;
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly IRegionManager _regionManager;
        private readonly IChatDbService _chatDbService;
        private readonly DispatcherTimer _checkTimer;
        private readonly IModelDownloadService _modelDownloadService;
        private CudaInfo _cudaInfo = new();
        private HardwareInfo _hardwareInfo = new();
        private ObservableCollection<InstallationStep> _installationSteps = new();
        private OllamaInfo _ollamaInfo = new()
        {
            IsRunning = false,
            HasError = false,
            Endpoint = "http://localhost:11434",
            InstalledModels = new ObservableCollection<OllamaModel>(),
        };

        private ModelRecommendation _modelRecommendation = new()
        {
            CanRunLargeModels = false,
            RecommendedModel = new ModelRequirements
            {
                Name = "初始化中...",
                Description = "正在检查系统状态...",
                MinimumVram = 0,
            },
            RecommendationReason = "正在检查系统状态...",
        };
        private bool _isChecking;
        private bool _showInstallationGuide = false;
        private string _checkingStatus = "正在加载系统信息...";

        public CudaInfo CudaInfo
        {
            get => _cudaInfo;
            set => SetProperty(ref _cudaInfo, value);
        }

        public HardwareInfo HardwareInfo
        {
            get => _hardwareInfo;
            set => SetProperty(ref _hardwareInfo, value);
        }

        public ObservableCollection<InstallationStep> InstallationSteps
        {
            get => _installationSteps;
            set => SetProperty(ref _installationSteps, value);
        }

        public OllamaInfo OllamaInfo
        {
            get => _ollamaInfo;
            set => SetProperty(ref _ollamaInfo, value);
        }

        public ModelRecommendation ModelRecommendation
        {
            get => _modelRecommendation;
            set => SetProperty(ref _modelRecommendation, value);
        }

        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }

        public bool ShowInstallationGuide
        {
            get => _showInstallationGuide;
            set => SetProperty(ref _showInstallationGuide, value);
        }

        public string CheckingStatus
        {
            get => _checkingStatus;
            set => SetProperty(ref _checkingStatus, value);
        }

        public DelegateCommand CheckSystemCommand { get; }
        public DelegateCommand ToggleGuideCommand { get; }
        public DelegateCommand OpenSetupCommand { get; }
        public ICommand NavigateToChatCommand { get; }
        public DelegateCommand<string> DeleteModelCommand { get; }
        public DelegateCommand StartOllamaCommand { get; }
        public DelegateCommand StopOllamaCommand { get; }

        public SystemStatusViewModel(
            ISystemCheckService systemCheckService,
            IHardwareMonitorService hardwareMonitorService,
            IRegionManager regionManager,
            IChatDbService chatDbService,
            IModelDownloadService modelDownloadService
        )
        {
            _systemCheckService = systemCheckService;
            _hardwareMonitorService = hardwareMonitorService;
            _regionManager = regionManager;
            _chatDbService = chatDbService;
            _modelDownloadService = modelDownloadService;
            // 初始化定时器
            _checkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30), // 每30秒检查一次
            };
            _checkTimer.Tick += async (s, e) =>
            {
                try
                {
                    var ollamaInfo = await _systemCheckService.CheckOllamaAsync();
                    if (!ollamaInfo.IsRunning && !IsChecking)
                    {
                        IsChecking = true;
                        await _systemCheckService.StartOllamaAsync();
                        await CheckSystem();
                        IsChecking = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"检查Ollama状态时出错: {ex.Message}");
                    IsChecking = false;
                }
            };

            // 设置默认值
            InitializeDefaultValues();

            CheckSystemCommand = new DelegateCommand(async () => await CheckSystem());
            ToggleGuideCommand = new DelegateCommand(
                () => ShowInstallationGuide = !ShowInstallationGuide
            );
            OpenSetupCommand = new DelegateCommand(
                () => _regionManager.RequestNavigate("MainRegion", "SystemSetupView")
            );
            NavigateToChatCommand = new DelegateCommand<string>(NavigateToChat);
            DeleteModelCommand = new DelegateCommand<string>(
                async (modelName) => await DeleteModel(modelName)
            );
            StartOllamaCommand = new DelegateCommand(async () => await StartOllama());
            StopOllamaCommand = new DelegateCommand(async () => await StopOllama());
            InitializeInstallationSteps();

            // 异步执行实际检查

            _ = Task.Run(async () => await CheckSystem());
            _checkTimer.Start();
        }

        private void InitializeDefaultValues()
        {
            // 设置默认的硬件信息
            HardwareInfo = new HardwareInfo
            {
                CpuName = "正在检测中...",
                CpuCores = 0,
                CpuUsage = 0,
                TotalMemory = 0,
                AvailableMemory = 0,
                MemoryUsage = 0,
                GpuName = "正在检测中...",
                GpuMemoryTotal = 0,
                GpuMemoryUsed = 0,
                GpuMemoryUsage = 0,
                Drives = new ObservableCollection<ollez.Models.DriveInfo>(),
            };

            // 设置默认的CUDA信息
            CudaInfo = new CudaInfo
            {
                IsAvailable = false,
                Version = "正在检测中...",
                DriverVersion = "正在检测中...",
                HasCudnn = false,
                CudnnVersion = "正在检测中...",
                Gpus = Array.Empty<GpuInfo>(),
            };

            // 设置默认的Ollama信息
            OllamaInfo = new OllamaInfo
            {
                IsRunning = false,
                HasError = false,
                Endpoint = "http://localhost:11434",
                Version = "正在检测中...",
                BuildType = "正在检测中...",
                InstallPath = "正在检测中...",
                ModelsPath = "正在检测中...",
                InstalledModels = new ObservableCollection<OllamaModel>(),
            };

            // 设置默认的模型推荐信息
            ModelRecommendation = new ModelRecommendation
            {
                CanRunLargeModels = false,
                RecommendedModel = new ModelRequirements
                {
                    Name = "正在分析系统配置...",
                    Description = "正在根据您的系统配置分析合适的模型...",
                    MinimumVram = 0,
                },
                RecommendationReason = "正在分析您的系统配置，稍后将为您推荐最适合的模型...",
            };

            // 初始化安装步骤状态
            InstallationSteps = new ObservableCollection<InstallationStep>
            {
                new InstallationStep(
                    "安装 NVIDIA 显卡驱动",
                    "正在检查驱动状态...",
                    "https://www.nvidia.com/download/index.aspx"
                ),
                new InstallationStep(
                    "安装 CUDA Toolkit",
                    "正在检查CUDA状态...",
                    "https://developer.nvidia.com/cuda-downloads"
                ),
                new InstallationStep(
                    "安装 Ollama",
                    "正在检查Ollama状态...",
                    "https://ollama.com/download"
                ),
                new InstallationStep("下载推荐模型", "等待系统环境检查完成...", string.Empty),
            };
        }

        private void InitializeInstallationSteps()
        {
            InstallationSteps = new ObservableCollection<InstallationStep>
            {
                new InstallationStep(
                    "安装 NVIDIA 显卡驱动",
                    "请确保您的系统已安装最新版本的NVIDIA显卡驱动。如果尚未安装，请访问NVIDIA官方网站下载并安装适合您显卡的最新驱动程序。",
                    "https://www.nvidia.com/download/index.aspx"
                ),
                new InstallationStep(
                    "安装 CUDA Toolkit",
                    "下载并安装CUDA Toolkit。请注意选择与您系统兼容的版本。安装完成后需要重启系统。",
                    "https://developer.nvidia.com/cuda-downloads"
                ),
                new InstallationStep(
                    "安装 Ollama",
                    "下载并安装Ollama。安装完成后，Ollama服务会自动启动并在后台运行。",
                    "https://ollama.com/download"
                ),
                new InstallationStep(
                    "下载推荐模型",
                    "打开终端或命令提示符，运行以下命令下载推荐的模型：\nollama pull [模型名称]\n\n下载完成后即可开始使用。",
                    string.Empty
                ),
            };
        }

        private async Task CheckSystem()
        {
            IsChecking = true;
            try
            {
                // 创建所有检查任务

                CheckingStatus = "正在检查硬件信息...";
                var hardwareTask = _hardwareMonitorService.GetHardwareInfoAsync();

                CheckingStatus = "正在检查CUDA状态...";
                var cudaTask = _systemCheckService.CheckCudaAsync();

                CheckingStatus = "正在检查Ollama状态...";
                var ollamaTask = _systemCheckService.CheckOllamaAsync();

                // 并行等待所有任务完成
                await Task.WhenAll(hardwareTask, cudaTask, ollamaTask);

                // 更新结果
                HardwareInfo = await hardwareTask;
                CudaInfo = await cudaTask;
                OllamaInfo = await ollamaTask;

                // 启动硬件监控
                _hardwareMonitorService.StartMonitoring();

                CheckingStatus = "正在加载Ollama配置...";
                LoadOllamaConfig();

                CheckingStatus = "正在获取模型推荐...";
                ModelRecommendation = await _systemCheckService.GetModelRecommendationAsync();

                // 更新安装步骤状态
                InstallationSteps[0].IsCompleted = CudaInfo.IsAvailable;
                InstallationSteps[1].IsCompleted = CudaInfo.IsAvailable;
                InstallationSteps[2].IsCompleted = OllamaInfo.IsRunning;
                InstallationSteps[3].IsCompleted =
                    OllamaInfo.IsRunning && OllamaInfo.InstalledModels.Count > 0;
            }
            finally
            {
                CheckingStatus = "加载完成";
                IsChecking = false;
            }
        }

        private async Task ExecuteOpenSetup()
        {
            var view = new SystemSetupView
            {
                DataContext = new SystemSetupViewModel(
                    _hardwareMonitorService,
                    _systemCheckService,
                    _chatDbService,
                    _modelDownloadService
                ),
            };
            await DialogHost.Show(view, "RootDialog");
        }

        private void NavigateToChat(string modelName)
        {
            var parameters = new NavigationParameters { { "SelectedModel", modelName } };
            _regionManager.RequestNavigate("MainRegion", "ChatView", parameters);
        }

        private async void LoadOllamaConfig()
        {
            var config = await _chatDbService.GetOllamaConfigAsync();
            if (config != null)
            {
                OllamaInfo.InstallPath = config.InstallPath;
                OllamaInfo.ModelsPath = config.ModelsPath;
            }
        }

        public async void SaveOllamaConfig()
        {
            var config = await _chatDbService.GetOllamaConfigAsync() ?? new OllamaConfig();
            config.InstallPath = OllamaInfo.InstallPath;
            config.ModelsPath = OllamaInfo.ModelsPath;
            await _chatDbService.SaveOllamaConfigAsync(config);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Debug.WriteLine("SystemStatusView - OnNavigatedTo");
            // 当导航到此视图时，确保数据已经加载
            _ = CheckSystem();

            // 开始监控

            _hardwareMonitorService.StartMonitoring();
            _checkTimer.Start();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 总是允许导航到此视图
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 当离开此视图时的处理逻辑
            Debug.WriteLine("SystemStatusView - OnNavigatedFrom");
            _hardwareMonitorService.StopMonitoring();
            _checkTimer.Stop();
        }

        private async Task DeleteModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return;

            try
            {
                IsChecking = true;
                CheckingStatus = $"正在删除模型 {modelName}...";

                // 调用系统服务删除模型
                await _systemCheckService.DeleteModelAsync(modelName);

                // 重新检查系统状态以更新UI
                await CheckSystem();
            }
            catch (Exception ex)
            {
                // 显示错误消息
                var errorContent = new TextBlock
                {
                    Text = $"删除模型时出错：{ex.Message}",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new System.Windows.Thickness(0, 0, 0, 8),
                };
                await DialogHost.Show(errorContent, "RootDialog");
            }
            finally
            {
                IsChecking = false;
            }
        }

        private async Task StartOllama()
        {
            try
            {
                IsChecking = true;
                CheckingStatus = "正在启动 Ollama...";
                await _systemCheckService.StartOllamaAsync();
                await CheckSystem();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动 Ollama 时出错: {ex.Message}");
            }
            finally
            {
                IsChecking = false;
            }
        }

        private async Task StopOllama()
        {
            try
            {
                IsChecking = true;
                CheckingStatus = "正在停止 Ollama...";
                
                // 先停止 ollama.exe
                var processOllama = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/F /IM ollama.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    }
                };
                
                // 再停止 ollama app.exe
                var processOllamaApp = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/F /IM \"ollama app.exe\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    }
                };

                try
                {
                    processOllama.Start();
                    await processOllama.WaitForExitAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"停止 ollama.exe 时出错: {ex.Message}");
                }

                try
                {
                    processOllamaApp.Start();
                    await processOllamaApp.WaitForExitAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"停止 ollama app.exe 时出错: {ex.Message}");
                }

                // 等待一会儿确保进程完全退出
                await Task.Delay(1000);
                await CheckSystem();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"停止 Ollama 时出错: {ex.Message}");
            }
            finally
            {
                IsChecking = false;
            }
        }
    }
}
