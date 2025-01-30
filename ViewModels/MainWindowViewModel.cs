using Prism.Commands;
using Prism.Mvvm;
using System.Threading.Tasks;
using YourNamespace.Services;

namespace YourNamespace.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ISystemCheckService _systemCheckService;
        
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

        public MainWindowViewModel(ISystemCheckService systemCheckService)
        {
            _systemCheckService = systemCheckService;
            CheckSystemCommand = new DelegateCommand(async () => await CheckSystemAsync());
            
            // 初始化时自动检查
            _ = CheckSystemAsync();
        }

        private async Task CheckSystemAsync()
        {
            IsChecking = true;
            CudaInfo = await _systemCheckService.CheckCudaAsync();
            OllamaInfo = await _systemCheckService.CheckOllamaAsync();
            IsChecking = false;
        }
    }
} 