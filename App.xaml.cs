using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using TermiusCN_Tool.Services;

namespace TermiusCN_Tool;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// 依赖注入服务容器
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 获取主窗口实例
    /// </summary>
    public static Window? MainWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindow = _window;
        _window.Activate();
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 注册服务（单例）
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<ITermiusDetector, TermiusDetector>();
        services.AddSingleton<IGitHubService, GitHubService>();
        services.AddSingleton<IFileManager, FileManager>();
        services.AddSingleton<IProcessManager, ProcessManager>();

        // 注册 ViewModels（瞬态）
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.BackupViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<ViewModels.LogViewModel>();

        return services.BuildServiceProvider();
    }
}
