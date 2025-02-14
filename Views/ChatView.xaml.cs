using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ollez.Views
{
    /// <summary>
    /// ChatView.xaml 的交互逻辑
    /// </summary>
    public partial class ChatView : UserControl
    {
        private bool _autoScroll = true;
        private double _previousScrollOffset;
        private bool _isSidebarExpanded = true;  // 修改为 true
        private const double EXPANDED_WIDTH = 280;
        private const double COLLAPSED_WIDTH = 0;

        public ChatView()
        {
            InitializeComponent();
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                var vm = DataContext as ViewModels.ChatViewModel;
                if (vm?.SendMessageCommand.CanExecute(null) == true)
                {
                    vm.SendMessageCommand.Execute(null);
                }
                e.Handled = true;
            }
        }

        private void ChatScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 检测用户是否手动滚动
            if (e.ExtentHeightChange == 0)
            {
                // 用户手动滚动
                _autoScroll = Math.Abs(e.VerticalOffset - e.ExtentHeight) < 10;
            }

            if (_autoScroll && e.ExtentHeightChange != 0)
            {
                ((ScrollViewer)sender).ScrollToBottom();
            }

            _previousScrollOffset = e.VerticalOffset;
        }

        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarExpanded = !_isSidebarExpanded;
            var targetWidth = _isSidebarExpanded ? EXPANDED_WIDTH : COLLAPSED_WIDTH;

            // 创建动画
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // 更新按钮图标
            var button = (Button)sender;
            var icon = (MaterialDesignThemes.Wpf.PackIcon)button.Content;
            icon.Kind = _isSidebarExpanded ?
                MaterialDesignThemes.Wpf.PackIconKind.ChevronLeft :
                MaterialDesignThemes.Wpf.PackIconKind.ChevronRight;

            // 应用动画
            LeftColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation);
        }
    }
}