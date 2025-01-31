using System.Windows.Controls;
using System.Windows.Navigation;
using ollez.ViewModels;

namespace ollez.Views
{
    public partial class LogView : UserControl
    {
        private LogViewModel _viewModel;
        private LogViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                {
                    _viewModel.ScrollToEndRequested -= OnScrollToEndRequested;
                }
                _viewModel = value;
                if (_viewModel != null)
                {
                    _viewModel.ScrollToEndRequested += OnScrollToEndRequested;
                }
            }
        }

        public LogView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => ViewModel = DataContext as LogViewModel;
        }

        private void OnScrollToEndRequested(object sender, System.EventArgs e)
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