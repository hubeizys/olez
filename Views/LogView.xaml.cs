using System.Windows.Controls;
using System.Windows.Navigation;
using ollez.ViewModels;

namespace ollez.Views
{
    public partial class LogView : UserControl
    {
        private LogViewModel ViewModel => DataContext as LogViewModel;

        public LogView()
        {
            InitializeComponent();
            Loaded += LogView_Loaded;
        }

        private void LogView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ScrollToEndRequested += (s, args) =>
                {
                    LogScrollViewer.ScrollToEnd();
                };
            }
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