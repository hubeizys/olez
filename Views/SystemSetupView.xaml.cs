using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics;

namespace ollez.Views
{
    public partial class SystemSetupView : UserControl
    {
        public SystemSetupView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}