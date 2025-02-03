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

namespace ollez.ViewModels
{
    public class InstallationStep
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 系统状态视图的视图模型
    /// </summary>
    public class SystemStatusViewModel : BindableBase, INavigationAware
    {
        private readonly ISystemCheckService _systemCheckService;
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private readonly IRegionManager _regionManager;
        private CudaInfo _cudaInfo = new();
        public CudaInfo CudaInfo
        {
            get => _cudaInfo;
            set => SetProperty(ref _cudaInfo, value);
        }

        private OllamaInfo _ollamaInfo = new()
        {
            IsRunning = false,
            HasError = false,
            Endpoint = "http://localhost:11434",
            InstalledModels = Array.Empty<OllamaModelInfo>()
        };
        public OllamaInfo OllamaInfo
        {
            get => _ollamaInfo;
            set => SetProperty(ref _ollamaInfo, value);
        }

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
        public ModelRecommendation ModelRecommendation
        {
            get => _modelRecommendation;
            set => SetProperty(ref _modelRecommendation, value);
        }

        private HardwareInfo _hardwareInfo;
        public HardwareInfo HardwareInfo
        {
            get => _hardwareInfo;
            set => SetProperty(ref _hardwareInfo, value);
        }

        private bool _isChecking;
        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }

        private bool _showInstallationGuide = false;
        public bool ShowInstallationGuide
        {
            get => _showInstallationGuide;
            set => SetProperty(ref _showInstallationGuide, value);
        }

        private ObservableCollection<InstallationStep> _installationSteps;
        public ObservableCollection<InstallationStep> InstallationSteps
        {
            get => _installationSteps;
            set => SetProperty(ref _installationSteps, value);
        }

        public DelegateCommand CheckSystemCommand { get; }
        public DelegateCommand ToggleGuideCommand { get; }
        public DelegateCommand OpenSetupCommand { get; }

        public SystemStatusViewModel(ISystemCheckService systemCheckService, IHardwareMonitorService hardwareMonitorService, IRegionManager regionManager)
        {
            _systemCheckService = systemCheckService;
            _hardwareMonitorService = hardwareMonitorService;
            _regionManager = regionManager;

            // 初始化硬件信息
            _hardwareInfo = new HardwareInfo();
            
            CheckSystemCommand = new DelegateCommand(async () => await CheckSystem());
            ToggleGuideCommand = new DelegateCommand(() => ShowInstallationGuide = !ShowInstallationGuide);
            OpenSetupCommand = new DelegateCommand(ExecuteOpenSetup);

            InitializeInstallationSteps();
            
            // 立即执行一次检查
            _ = CheckSystem();
        }

        private void InitializeInstallationSteps()
        {
            InstallationSteps = new ObservableCollection<InstallationStep>
            {
                new InstallationStep
                {
                    Title = "安装 NVIDIA 显卡驱动",
                    Description = "请确保您的系统已安装最新版本的NVIDIA显卡驱动。如果尚未安装，请访问NVIDIA官方网站下载并安装适合您显卡的最新驱动程序。",
                    Link = "https://www.nvidia.com/download/index.aspx",
                    IsCompleted = false
                },
                new InstallationStep
                {
                    Title = "安装 CUDA Toolkit",
                    Description = "下载并安装CUDA Toolkit。请注意选择与您系统兼容的版本。安装完成后需要重启系统。",
                    Link = "https://developer.nvidia.com/cuda-downloads",
                    IsCompleted = false
                },
                new InstallationStep
                {
                    Title = "安装 Ollama",
                    Description = "下载并安装Ollama。安装完成后，Ollama服务会自动启动并在后台运行。",
                    Link = "https://ollama.com/download",
                    IsCompleted = false
                },
                new InstallationStep
                {
                    Title = "下载推荐模型",
                    Description = "打开终端或命令提示符，运行以下命令下载推荐的模型：\nollama pull [模型名称]\n\n下载完成后即可开始使用。",
                    IsCompleted = false
                }
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
                ModelRecommendation = await _systemCheckService.GetModelRecommendationAsync();
                
                // 更新安装步骤状态
                InstallationSteps[0].IsCompleted = CudaInfo.IsAvailable;
                InstallationSteps[1].IsCompleted = CudaInfo.IsAvailable;
                InstallationSteps[2].IsCompleted = OllamaInfo.IsRunning;
                InstallationSteps[3].IsCompleted = OllamaInfo.IsRunning && OllamaInfo.InstalledModels.Length > 0;
            }
            finally
            {
                IsChecking = false;
            }
        }

        private async void ExecuteOpenSetup()
        {
            var view = new SystemSetupView();
            var dialog = view;
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog", new DialogOpenedEventHandler((sender, args) =>
            {
                if (dialog.DataContext is ViewModelBase viewModel)
                {
                    viewModel.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "DialogResult" && args.Session.IsEnded == false)
                        {
                            args.Session.Close();
                        }
                    };
                }
            }));
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