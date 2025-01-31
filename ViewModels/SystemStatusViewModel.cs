/*
 * 文件名：SystemStatusViewModel.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：系统状态视图的视图模型
 */

using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Threading.Tasks;
using System.Diagnostics;
using ollez.Services;

namespace ollez.ViewModels
{
    /// <summary>
    /// 系统状态视图的视图模型
    /// </summary>
    public class SystemStatusViewModel : BindableBase, INavigationAware
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

        public SystemStatusViewModel(ISystemCheckService systemCheckService)
        {
            _systemCheckService = systemCheckService;
            CheckSystemCommand = new DelegateCommand(async () => await CheckSystemAsync());
        }

        private async Task CheckSystemAsync()
        {
            IsChecking = true;
            CudaInfo = await _systemCheckService.CheckCudaAsync();
            OllamaInfo = await _systemCheckService.CheckOllamaAsync();
            IsChecking = false;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Debug.WriteLine("SystemStatusView - OnNavigatedTo");
            // 当导航到此视图时，确保数据已经加载
            _ = CheckSystemAsync();
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
        }
    }
}