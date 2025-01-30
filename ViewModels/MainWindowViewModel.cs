using Prism.Mvvm;

namespace YourNamespace.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism with Material Design";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {
        }
    }
} 