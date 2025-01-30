using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using YourNamespace.Views;
using YourNamespace.Services;

namespace YourNamespace
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ISystemCheckService, SystemCheckService>();
        }
    }
} 