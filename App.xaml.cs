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
        public App()
        {
            // 必须先于基类构造函数执行！
            InitializeTraceSources();
            InitializeComponent();
        }

        private static void InitializeTraceSources()
        {// 增加这行代码确保跟踪源刷新
            PresentationTraceSources.Refresh();

            // 清除原有监听器
            PresentationTraceSources.DataBindingSource.Listeners.Clear();

            // 添加调试输出监听器（需要更严格的配置）
            PresentationTraceSources.DataBindingSource.Listeners.Add(
                new DebugTraceListener
                {
                    TraceOutputOptions = TraceOptions.DateTime | TraceOptions.LogicalOperationStack,
                    Filter = new EventTypeFilter(SourceLevels.All) // 关键！添加过滤器
                }
            );

            // 必须设置为 Verbose 级别
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Verbose;
        }
        /// <summary>
        /// 创建主窗口
        /// </summary>
        /// <returns>应用程序的主窗口</returns>
        protected override Window CreateShell()
        {
            // 初始化 Serilog
            //var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app_.log");
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Debug()
            //    .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            //    .WriteTo.File(logPath,
            //        rollingInterval: RollingInterval.Day,
            //        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            //        shared: true,
            //        flushToDiskInterval: TimeSpan.FromSeconds(1))
            //    .CreateLogger();

            //Log.Information("应用程序启动");

            return Container.Resolve<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AllocConsole(); // 显示控制台窗口
            base.OnStartup(e);
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        /// <summary>
        /// 注册依赖注入的服务和视图
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册数据库上下文工厂
            containerRegistry.Register<ChatDbContext>(() =>
            {
                var context = new ChatDbContext();
                context.Database.EnsureCreated();
                return context;
            });
            containerRegistry.Register<Func<ChatDbContext>>(container => () => container.Resolve<ChatDbContext>());
            containerRegistry.Register<IChatDbService, ChatDbService>();

            // 注册其他服务
            containerRegistry.RegisterSingleton<ISystemCheckService, SystemCheckService>();
            containerRegistry.RegisterSingleton<IChatService, ChatService>();
            containerRegistry.RegisterSingleton<ILogService, LogService>();
            containerRegistry.RegisterSingleton<IHardwareMonitorService, HardwareMonitorService>();

            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<SystemStatusView, SystemStatusViewModel>();
            containerRegistry.RegisterForNavigation<ChatView, ChatViewModel>();
            containerRegistry.RegisterForNavigation<LogView, LogViewModel>();
            containerRegistry.RegisterForNavigation<AboutView, AboutViewModel>();
            containerRegistry.RegisterForNavigation<SystemSetupView, SystemSetupViewModel>();

            containerRegistry.RegisterScoped<SystemStatusViewModel>();
            containerRegistry.RegisterScoped<SystemSetupViewModel>();
        }
    }
}