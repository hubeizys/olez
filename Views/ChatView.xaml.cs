using System.Windows.Controls;
using System.Windows.Input;

namespace ollez.Views
{
    public partial class ChatView : UserControl
    {
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
    }
} 