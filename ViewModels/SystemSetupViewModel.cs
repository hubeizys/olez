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

namespace ollez.ViewModels
{
    public class SystemSetupViewModel : BindableBase
    {
        private readonly IHardwareMonitorService _hardwareMonitorService;
        private int _currentStep;
        private bool _hasNvidia;
        private string _selectedDrive = string.Empty;
        private ObservableCollection<string> _availableDrives = new();
        private string _selectedInstallPath = @"D:\Ollama";
        private bool _hasLocalSetup;
        private string _localSetupPath;
        private string _link = "https://ollama.com/download";

        public SystemSetupViewModel(IHardwareMonitorService hardwareMonitorService)
        {
            _hardwareMonitorService = hardwareMonitorService;
            _currentStep = 0;
            
            NextCommand = new DelegateCommand(ExecuteNext, CanExecuteNext);
            PreviousCommand = new DelegateCommand(ExecutePrevious, CanExecutePrevious);
            SkipCommand = new DelegateCommand(ExecuteSkip, CanExecuteSkip);
            SelectInstallPathCommand = new DelegateCommand(ExecuteSelectInstallPath);
            InstallOllamaCommand = new DelegateCommand(ExecuteInstallOllama);
            OpenLocalSetupFolderCommand = new DelegateCommand(ExecuteOpenLocalSetupFolder);
            
            InitializeDrives();

            // 检查本地是否存在Ollama安装包
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var ollamaSetupPath = Path.Combine(appDir, "ollama", "OllamaSetup.exe");
            if (File.Exists(ollamaSetupPath))
            {
                HasLocalSetup = true;
                LocalSetupPath = ollamaSetupPath;
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

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand SkipCommand { get; }
        public DelegateCommand SelectInstallPathCommand { get; }
        public DelegateCommand InstallOllamaCommand { get; }
        public DelegateCommand OpenLocalSetupFolderCommand { get; }

        private void ExecuteNext()
        {
            if (CurrentStep < 2)
            {
                CurrentStep++;
            }
        }

        private bool CanExecuteNext()
        {
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安装Ollama时出错: {ex.Message}");
                MessageBox.Show($"安装Ollama时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteOpenLocalSetupFolder()
        {
            if (!string.IsNullOrEmpty(LocalSetupPath) && File.Exists(LocalSetupPath))
            {
                Process.Start("explorer.exe", $"/select,\"{LocalSetupPath}\"");
            }
        }
    }
} 