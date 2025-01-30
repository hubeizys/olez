using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using YourNamespace.Views;

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
            // 在这里注册你的服务和视图
        }
    }
} 