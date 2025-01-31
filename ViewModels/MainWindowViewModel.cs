using Prism.Commands;
using Prism.Mvvm;
using System.Threading.Tasks;
using ollez.Services;
using Prism.Regions;

namespace ollez.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ISystemCheckService _systemCheckService;
        private readonly IRegionManager _regionManager;
        
        private CudaInfo _cudaInfo;
        public CudaInfo CudaInfo
        {
            get => _cudaInfo;
            set => SetProperty(ref _cudaInfo, value);
        }

        private OllamaInfo _ollamaInfo;
        public OllamaInfo OllamaInfo
        {
            get => _ollamaInfo;
            set => SetProperty(ref _ollamaInfo, value);
        }

        private bool _isChecking;
        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }

        public DelegateCommand CheckSystemCommand { get; }
        public DelegateCommand<string> NavigateCommand { get; }

        public MainWindowViewModel(ISystemCheckService systemCheckService, IRegionManager regionManager)
        {
            _systemCheckService = systemCheckService;
            _regionManager = regionManager;
            CheckSystemCommand = new DelegateCommand(async () => await CheckSystemAsync());
            NavigateCommand = new DelegateCommand<string>(Navigate);
            
            // 初始化时自动检查
            _ = CheckSystemAsync();

            // 默认导航到系统状态页面
            Navigate("SystemStatusView");
        }

        private async Task CheckSystemAsync()
        {
            IsChecking = true;
            CudaInfo = await _systemCheckService.CheckCudaAsync();
            OllamaInfo = await _systemCheckService.CheckOllamaAsync();
            IsChecking = false;
        }

        private void Navigate(string viewName)
        {
            _regionManager.RequestNavigate("ContentRegion", viewName);
        }
    }
} 