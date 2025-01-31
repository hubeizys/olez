using Prism.Mvvm;

namespace ollez.ViewModels
{
    public class AboutViewModel : BindableBase
    {
        private string _version = "0.0.1";
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private string _creator = "凶残的朱哥";
        public string Creator
        {
            get => _creator;
            set => SetProperty(ref _creator, value);
        }

        private string _email = "laozhu_shangxin@hotmail.com";
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _copyright = "Copyright © 2024";
        public string Copyright
        {
            get => _copyright;
            set => SetProperty(ref _copyright, value);
        }
    }
}