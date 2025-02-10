/*
 * 文件名：App.xaml.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：应用程序入口点
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
using ollez.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app_.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                //.WriteTo.File(logPath,
                //  rollingInterval: RollingInterval.Day,
                //  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                //  shared: true,
                //  flushToDiskInterval: TimeSpan.FromSeconds(1))
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
            // 首先注册所有基础服务（注意顺序：被依赖的服务需要先注册）
            containerRegistry.RegisterSingleton<ILogService, LogService>();
            containerRegistry.RegisterSingleton<ISystemCheckService, SystemCheckService>();
            containerRegistry.RegisterSingleton<IHardwareMonitorService, HardwareMonitorService>();
            containerRegistry.RegisterSingleton<IChatService, ChatService>();
            containerRegistry.RegisterSingleton<IModelDownloadService, ModelDownloadService>();

            // 注册数据库相关服务
            containerRegistry.Register<ChatDbContext>(() =>
            {
                var context = new ChatDbContext();
                context.Database.EnsureCreated();
                return context;
            });
            containerRegistry.Register<Func<ChatDbContext>>(container => () => container.Resolve<ChatDbContext>());
            containerRegistry.Register<IChatDbService, ChatDbService>();

            // 注册视图模型
            containerRegistry.RegisterScoped<SystemStatusViewModel>();
            containerRegistry.RegisterScoped<SystemSetupViewModel>();

            // 最后注册导航
            containerRegistry.RegisterForNavigation<SystemStatusView, SystemStatusViewModel>();
            containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
            containerRegistry.RegisterForNavigation<LogView, LogViewModel>();
            containerRegistry.RegisterForNavigation<AboutView, AboutViewModel>();
            containerRegistry.RegisterForNavigation<SystemSetupView, SystemSetupViewModel>();
        }
    }
}
