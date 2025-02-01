using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ollez.Views
{
    /// <summary>
    /// ChatView.xaml 的交互逻辑
    /// </summary>
    public partial class ChatView : UserControl
    {
        private bool _autoScroll = true;
        private double _previousScrollOffset;

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
    }
}