/*
 * 文件名：App.xaml.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：应用程序入口点，负责依赖注入和初始化
 */

using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using ollez.Views;
using ollez.Services;

namespace ollez
{
    /// <summary>
    /// 应用程序主类，继承自PrismApplication
    /// </summary>
    public partial class App : PrismApplication
    {
        /// <summary>
        /// 创建主窗口
        /// </summary>
        /// <returns>应用程序的主窗口</returns>
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        /// <summary>
        /// 注册依赖注入的服务和视图
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ISystemCheckService, SystemCheckService>();
            containerRegistry.RegisterSingleton<IChatService, ChatService>();

            containerRegistry.RegisterForNavigation<SystemStatusView, SystemStatusViewModel>();
            containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
        }
    }
} 