using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using TermiusCN_Tool.Helpers;
using TermiusCN_Tool.Models;
using TermiusCN_Tool.Services;
using WinRT.Interop;

namespace TermiusCN_Tool.ViewModels;

/// <summary>
/// 设置页面 ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private AppConfig _config = new();

    [ObservableProperty]
    private string customPath = string.Empty;

    [ObservableProperty]
    private int defaultTypeIndex = 1;

    [ObservableProperty]
    private bool autoCheckUpdate = true;

    [ObservableProperty]
    private bool showWarnings = true;

    [ObservableProperty]
    private string proxyAddress = string.Empty;

    [ObservableProperty]
    private int proxyPort = 0;

    [ObservableProperty]
    private bool isTesting;

    public SettingsViewModel(IConfigService configService)
    {
        _configService = configService;
        _ = LoadConfigAsync();
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            _config = await _configService.LoadConfigAsync();

            CustomPath = _config.CustomTermiusPath;
            DefaultTypeIndex = (int)_config.DefaultLocalizeType;
            AutoCheckUpdate = _config.AutoCheckUpdate;
            ShowWarnings = _config.ShowWarnings;
            ProxyAddress = _config.ProxyAddress;
            ProxyPort = _config.ProxyPort;
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("加载配置失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            _config.CustomTermiusPath = CustomPath;
            _config.DefaultLocalizeType = (LocalizeType)DefaultTypeIndex;
            _config.AutoCheckUpdate = AutoCheckUpdate;
            _config.ShowWarnings = ShowWarnings;
            _config.ProxyAddress = ProxyAddress;
            _config.ProxyPort = ProxyPort;

            await _configService.SaveConfigAsync(_config);

            await DialogHelper.ShowSuccessAsync("保存成功", "配置已保存");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("保存失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task Browse()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            // 获取主窗口句柄
            if (App.MainWindow != null)
            {
                var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(picker, hwnd);
            }

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                CustomPath = folder.Path;
            }
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("选择文件夹失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ClearAllData()
    {
        var confirm = await DialogHelper.ShowConfirmAsync(
            "清除所有数据",
            "确定要清除所有应用数据吗？\n\n这将删除：\n1. 所有备份文件\n2. 用户配置文件\n3. 缓存文件\n\n此操作不可撤销！清除后应用将关闭。");

        if (!confirm) return;

        try
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TermiusCN-Tool");

            if (System.IO.Directory.Exists(appDataDir))
            {
                System.IO.Directory.Delete(appDataDir, true);
            }

            await DialogHelper.ShowSuccessAsync("清除成功", "所有数据已清除，应用即将退出。");
            
            // 退出应用
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("清除失败", $"无法清除数据: {ex.Message}\n\n请尝试手动删除文件夹:\n%AppData%\\TermiusCN-Tool");
        }
    }

    [RelayCommand]
    private async Task TestProxy()
    {
        IsTesting = true;
        try
        {
            var testConfig = new AppConfig
            {
                ProxyAddress = ProxyAddress,
                ProxyPort = ProxyPort
            };

            var (success, message) = await HttpHelper.TestConnectionAsync(testConfig);

            if (success)
            {
                await DialogHelper.ShowSuccessAsync("连接成功", "代理配置正常，可以访问 GitHub API");
            }
            else
            {
                await DialogHelper.ShowErrorAsync("连接失败", message);
            }
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("测试失败", ex.Message);
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task ResetToDefault()
    {
        var confirm = await DialogHelper.ShowConfirmAsync(
            "确认重置",
            "确定要将所有设置恢复为默认值吗？");

        if (!confirm) return;

        _config = new AppConfig();
        CustomPath = _config.CustomTermiusPath;
        DefaultTypeIndex = (int)_config.DefaultLocalizeType;
        AutoCheckUpdate = _config.AutoCheckUpdate;
        ShowWarnings = _config.ShowWarnings;
        ProxyAddress = _config.ProxyAddress;
        ProxyPort = _config.ProxyPort;

        await DialogHelper.ShowSuccessAsync("已重置", "设置已恢复为默认值，点击保存按钮应用更改");
    }
}
