using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using ollez.Services;
using Prism.Regions;
using System.Diagnostics;
using ollez.Models;
using System.Collections.ObjectModel;

namespace ollez.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ISystemCheckService _systemCheckService;
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
            InstalledModels = new ObservableCollection<OllamaModel>()
        };
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

            // 初始化时自动检查并导航到默认页面

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                await CheckSystemAsync();
                // 确保 UI 更新在主线程上执行
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Navigate("SystemStatusView");
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Initialization error: {ex}");
            }
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
            Debug.WriteLine($"Navigate: {viewName}");
            try
            {
                _regionManager.RequestNavigate("ContentRegion", viewName, navigationResult =>
                {
                    if (navigationResult.Error != null)
                    {
                        Debug.WriteLine($"Navigation Error: {navigationResult.Error}");
                        Debug.WriteLine($"Error Stack Trace: {navigationResult.Error.StackTrace}");
                    }
                    Debug.WriteLine($"Navigation completed: {navigationResult.Result}, Error: {navigationResult.Error?.Message}");
                });
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Navigation exception: {ex}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}

