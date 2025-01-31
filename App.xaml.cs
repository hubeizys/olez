using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using ollez.Views;
using ollez.Services;

namespace ollez
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