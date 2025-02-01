using System.Windows.Controls;
using System.Windows.Navigation;
using ollez.ViewModels;

namespace ollez.Views
{
    public partial class LogView : UserControl
    {
        private LogViewModel _viewModel = null!;
        private LogViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                {
                    _viewModel.ScrollToEndRequested -= OnScrollToEndRequested;
                }
                
                if (value is LogViewModel viewModel)
                {
                    _viewModel = viewModel;
                    _viewModel.ScrollToEndRequested += OnScrollToEndRequested;
                }
            }
        }

        public LogView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is LogViewModel vm)
                {
                    ViewModel = vm;
                }
            };
        }

        private void OnScrollToEndRequested(object? sender, System.EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                LogScrollViewer?.ScrollToEnd();
            });
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}