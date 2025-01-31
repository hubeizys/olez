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
using ollez.ViewModels;
using Serilog;
using System.IO;
using System;

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
            // 初始化 Serilog
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("应用程序启动");
            
            return Container.Resolve<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        /// <summary>
        /// 注册依赖注入的服务和视图
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ISystemCheckService, SystemCheckService>();
            containerRegistry.RegisterSingleton<IChatService, ChatService>();
            containerRegistry.RegisterSingleton<ILogService, LogService>();

            containerRegistry.RegisterForNavigation<SystemStatusView, SystemStatusViewModel>();
            containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
            containerRegistry.RegisterForNavigation<LogView, LogViewModel>();
        }
    }
} 