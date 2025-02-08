using System.Diagnostics;
using System.Windows.Navigation;

namespace ollez.Views
{
    public partial class AboutView
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri.Scheme == "mailto")
            {
                // �����ʼ������߼�
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            else
            {
                // ������ͨ��ַ�����߼�
                Process.Start(new ProcessStartInfo("explorer.exe", e.Uri.AbsoluteUri));
            }
            e.Handled = true;
        }
    }
}
