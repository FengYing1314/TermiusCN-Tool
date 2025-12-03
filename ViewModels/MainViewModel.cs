using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using TermiusCN_Tool.Helpers;
using TermiusCN_Tool.Models;
using TermiusCN_Tool.Services;

namespace TermiusCN_Tool.ViewModels;

/// <summary>
/// 主页面 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ITermiusDetector _detector;
    private readonly IGitHubService _githubService;
    private readonly IFileManager _fileManager;
    private readonly IProcessManager _processManager;
    private readonly IConfigService _configService;

    [ObservableProperty]
    private string currentVersion = "检测中...";

    [ObservableProperty]
    private string availableVersion = "检查中...";

    [ObservableProperty]
    private int selectedTypeIndex = 1; // 默认选择"试用版"

    [ObservableProperty]
    private bool isWorking;

    [ObservableProperty]
    private bool canLocalize = true;

    [ObservableProperty]
    private string progressMessage = "";

    [ObservableProperty]
    private InfoBarSeverity statusSeverity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private string statusMessage = "准备就绪";

    [ObservableProperty]
    private string debugInfo = "";

    private TermiusInfo? _terminusInfo;

    public MainViewModel(
        ITermiusDetector detector,
        IGitHubService githubService,
        IFileManager fileManager,
        IProcessManager processManager,
        IConfigService configService)
    {
        _detector = detector;
        _githubService = githubService;
        _fileManager = fileManager;
        _processManager = processManager;
        _configService = configService;

        // 初始化时检测
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var config = await _configService.LoadConfigAsync();
        SelectedTypeIndex = (int)config.DefaultLocalizeType;

        await DetectTermiusAsync();
        await CheckLatestVersionAsync();
    }

    [RelayCommand]
    private async Task Localize()
    {
        try
        {
            IsWorking = true;
            CanLocalize = false;

            // 1. 检测 Termius
            ProgressMessage = "正在检测 Termius...";
            await DetectTermiusAsync();

            if (_terminusInfo == null || !_terminusInfo.IsFound)
            {
                await DialogHelper.ShowErrorAsync("未找到 Termius", "请确保已安装 Termius，或在设置中指定自定义路径");
                return;
            }

            // 2. 关闭进程
            ProgressMessage = "正在关闭 Termius 进程...";
            if (_processManager.IsTermiusRunning())
            {
                var closed = await _processManager.CloseTermiusAsync(5);
                if (!closed)
                {
                    var confirm = await DialogHelper.ShowConfirmAsync(
                        "进程关闭失败",
                        "Termius 进程无法正常关闭，是否强制结束？");

                    if (confirm)
                    {
                        _processManager.KillTermius();
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // 3. 备份
            ProgressMessage = "正在备份原文件...";
            await _fileManager.BackupAsync(_terminusInfo.AsarPath, _terminusInfo.Version);

            // 4. 下载
            ProgressMessage = "正在下载汉化文件...";
            var localizeType = (LocalizeType)SelectedTypeIndex;
            var tempFile = await _githubService.DownloadLocalizeFileAsync(_terminusInfo.Version, localizeType);

            // 5. 替换
            ProgressMessage = "正在替换文件...";
            await _fileManager.ReplaceAsync(tempFile, _terminusInfo.AsarPath);

            // 6. 完成
            ProgressMessage = "汉化完成！";
            StatusSeverity = InfoBarSeverity.Success;
            StatusMessage = "汉化成功！请重启 Termius 使汉化生效";

            await DialogHelper.ShowRestartPromptAsync();
        }
        catch (Exception ex)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = $"汉化失败: {ex.Message}";
            await DialogHelper.ShowErrorAsync("操作失败", ex.Message);
        }
        finally
        {
            IsWorking = false;
            CanLocalize = true;
            ProgressMessage = "";
        }
    }

    private async Task DetectTermiusAsync()
    {
        try
        {
            var config = await _configService.LoadConfigAsync();
            _terminusInfo = await _detector.DetectAsync(config.CustomTermiusPath);

            if (_terminusInfo.IsFound)
            {
                CurrentVersion = _terminusInfo.Version;
                StatusSeverity = InfoBarSeverity.Success;
                StatusMessage = $"✅ 已检测到 Termius {_terminusInfo.Version} - 安装路径: {_terminusInfo.InstallPath}";
                CanLocalize = true;
            }
            else
            {
                CurrentVersion = "未检测到 Termius";
                StatusSeverity = InfoBarSeverity.Warning;
                
                // 提供详细的检测信息
                var isRunning = _processManager.IsTermiusRunning();
                var processPath = _processManager.GetTermiusInstallPathFromProcess();
                
                if (isRunning && !string.IsNullOrEmpty(processPath))
                {
                    StatusMessage = $"⚠️ 检测到 Termius 进程运行中 (路径: {processPath})，但验证失败。请检查安装是否完整。";
                }
                else if (isRunning)
                {
                    StatusMessage = "⚠️ 检测到 Termius 进程，但无法获取安装路径。请尝试以管理员身份运行本工具。";
                }
                else
                {
                    StatusMessage = "⚠️ 未找到 Termius。请先启动 Termius 或在设置中手动指定安装路径。";
                }
                
                CanLocalize = false;
            }
        }
        catch (Exception ex)
        {
            CurrentVersion = "检测失败";
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = $"❌ 检测错误: {ex.Message}";
            CanLocalize = false;
        }
    }

    private async Task CheckLatestVersionAsync()
    {
        try
        {
            var version = await _githubService.GetLatestVersionAsync();
            if (!string.IsNullOrEmpty(version))
            {
                AvailableVersion = $"最新版本: {version}";
            }
            else
            {
                AvailableVersion = "检查失败";
            }
        }
        catch
        {
            AvailableVersion = "检查失败";
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentVersion = "检测中...";
        AvailableVersion = "检查中...";
        StatusMessage = "正在刷新...";
        StatusSeverity = InfoBarSeverity.Informational;

        await DetectTermiusAsync();
        await CheckLatestVersionAsync();
    }

    [RelayCommand]
    private async Task ShowDebugInfo()
    {
        var config = await _configService.LoadConfigAsync();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var defaultPath = System.IO.Path.Combine(localAppData, @"Programs\Termius");
        
        var isRunning = _processManager.IsTermiusRunning();
        var processPath = _processManager.GetTermiusInstallPathFromProcess();
        
        var debugText = $"=== Termius 检测调试信息 ===\n\n";
        debugText += $"1. 自定义路径: {(string.IsNullOrEmpty(config.CustomTermiusPath) ? "未设置" : config.CustomTermiusPath)}\n";
        debugText += $"   存在: {(!string.IsNullOrEmpty(config.CustomTermiusPath) && System.IO.Directory.Exists(config.CustomTermiusPath))}\n\n";
        
        debugText += $"2. 默认路径: {defaultPath}\n";
        debugText += $"   存在: {System.IO.Directory.Exists(defaultPath)}\n";
        if (System.IO.Directory.Exists(defaultPath))
        {
            var exePath = System.IO.Path.Combine(defaultPath, "Termius.exe");
            var resourcesPath = System.IO.Path.Combine(defaultPath, "resources");
            var asarPath = System.IO.Path.Combine(resourcesPath, "app.asar");
            
            debugText += $"   Termius.exe: {System.IO.File.Exists(exePath)}\n";
            debugText += $"   resources文件夹: {System.IO.Directory.Exists(resourcesPath)}\n";
            debugText += $"   app.asar: {System.IO.File.Exists(asarPath)}\n";
        }
        debugText += $"\n";
        
        debugText += $"3. 进程检测:\n";
        debugText += $"   Termius运行中: {isRunning}\n";
        debugText += $"   进程路径: {(string.IsNullOrEmpty(processPath) ? "无法获取" : processPath)}\n";
        if (!string.IsNullOrEmpty(processPath))
        {
            debugText += $"   路径存在: {System.IO.Directory.Exists(processPath)}\n";
        }
        
        await DialogHelper.ShowErrorAsync("调试信息", debugText);
    }
}
