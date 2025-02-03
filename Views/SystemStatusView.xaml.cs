using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Navigation;
using Serilog;

namespace ollez.Views
{
    public partial class SystemStatusView : UserControl
    {
        public SystemStatusView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
} 