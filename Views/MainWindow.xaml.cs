/*
 * 文件名：MainWindow.xaml.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：主窗口的代码后台，处理窗口的基本操作如拖动、最小化、最大化和关闭
 */

using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Windows.Input;

namespace ollez.Views
{
    /// <summary>
    /// MainWindow 的交互逻辑，处理窗口的基本操作
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 初始化主窗口
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 处理窗口拖动事件
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        /// <summary>
        /// 处理窗口最小化按钮点击事件
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 处理窗口最大化/还原按钮点击事件
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        /// <summary>
        /// 处理窗口关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 处理超链接导航请求事件
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
} 