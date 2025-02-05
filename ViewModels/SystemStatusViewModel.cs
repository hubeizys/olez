/*
 * 文件名：SystemStatusViewModel.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：系统状态视图的视图模型
 */

using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;
using ollez.Services;
using ollez.Models;
using MaterialDesignThemes.Wpf;
using ollez.Views;
using System.Windows.Input;
using ollez.Data;
using System.Linq;

namespace ollez.ViewModels
{
    public class InstallationStep
    {
        public string Title { get; }
        public string Description { get; }
        public string Link { get; }
        public bool IsCompleted { get; set; }

        public InstallationStep(string title, string description, string link, bool isCompleted = false)
        {
            Title = title;
            Description = description;
            Link = link;
            IsCompleted = isCompleted;
        }
    }

    /// <summary>
    /// 系统状态视图的视图模型
    /// </summary>
    public class SystemStatusViewModel : BindableBase, INavigationAware
    {
        private readonly ISystemCheckService _systemCheckService;
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly IRegionManager _regionManager;
        private readonly IChatDbService _chatDbService;
        private CudaInfo _cudaInfo = new();
        private HardwareInfo _hardwareInfo = new();
        private ObservableCollection<InstallationStep> _installationSteps = new();
        private OllamaInfo _ollamaInfo = new()
        {
            IsRunning = false,
            HasError = false,
            Endpoint = "http://localhost:11434",
            InstalledModels = new ObservableCollection<OllamaModel>()
        };

        private ModelRecommendation _modelRecommendation = new()
        {
            CanRunLargeModels = false,
            RecommendedModel = new ModelRequirements
            {
                Name = "初始化中...",
                Description = "正在检查系统状态...",
                MinimumVram = 0
            },
            RecommendationReason = "正在检查系统状态..."
        };
        private bool _isChecking;
        private bool _showInstallationGuide = false;

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

        public DelegateCommand CheckSystemCommand { get; }
        public DelegateCommand ToggleGuideCommand { get; }
        public DelegateCommand OpenSetupCommand { get; }
        public ICommand NavigateToChatCommand { get; }

        public SystemStatusViewModel(ISystemCheckService systemCheckService, IHardwareMonitorService hardwareMonitorService, IRegionManager regionManager, IChatDbService chatDbService)
        {
            _systemCheckService = systemCheckService;
            _hardwareMonitorService = hardwareMonitorService;
            _regionManager = regionManager;
            _chatDbService = chatDbService;

            // 初始化硬件信息
            _hardwareInfo = new HardwareInfo();
            
            CheckSystemCommand = new DelegateCommand(async () => await CheckSystem());
            ToggleGuideCommand = new DelegateCommand(() => ShowInstallationGuide = !ShowInstallationGuide);
            OpenSetupCommand = new DelegateCommand(async () => await ExecuteOpenSetup());
            NavigateToChatCommand = new DelegateCommand<string>(NavigateToChat);
            InitializeInstallationSteps();
            
            // 立即执行一次检查
            _ = CheckSystem();

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
                )
            };
        }

        private async Task CheckSystem()
        {
            IsChecking = true;

            try
            {
                // 获取硬件信息
                HardwareInfo = await _hardwareMonitorService.GetHardwareInfoAsync();
                _hardwareMonitorService.StartMonitoring();

                // 执行其他现有的检查...
                CudaInfo = await _systemCheckService.CheckCudaAsync();
                OllamaInfo = await _systemCheckService.CheckOllamaAsync();

                // 初始化时加载Ollama配置
                LoadOllamaConfig();
                ModelRecommendation = await _systemCheckService.GetModelRecommendationAsync();
                

                // 更新安装步骤状态
                InstallationSteps[0].IsCompleted = CudaInfo.IsAvailable;
                InstallationSteps[1].IsCompleted = CudaInfo.IsAvailable;
                InstallationSteps[2].IsCompleted = OllamaInfo.IsRunning;
                InstallationSteps[3].IsCompleted = OllamaInfo.IsRunning && OllamaInfo.InstalledModels.Count > 0;
            }
            finally


            {
                IsChecking = false;
            }
        }

        private async Task ExecuteOpenSetup()
        {
            var view = new SystemSetupView { DataContext = new SystemSetupViewModel(_hardwareMonitorService, _systemCheckService, _chatDbService) };
            await DialogHost.Show(view, "RootDialog");
        }

        private void NavigateToChat(string modelName)
        {
            var parameters = new NavigationParameters
            {
                { "SelectedModel", modelName }
            };
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
        }
    }
}
