using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TermiusCN_Tool.Helpers;
using TermiusCN_Tool.Services;
using TermiusCN_Tool.ViewModels;

namespace TermiusCN_Tool.Views;

/// <summary>
/// 主页面
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        this.InitializeComponent();

        // 从依赖注入容器获取 ViewModel
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
    }

    private async void Border_DragOver(object sender, DragEventArgs e)
    {
        // 检查是否包含文件
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "替换汉化文件";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
        }
        else
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }
    }

    private async void Border_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            try
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0 && items[0] is Windows.Storage.StorageFile file)
                {
                    // 检查文件扩展名
                    if (!file.Name.EndsWith(".asar", StringComparison.OrdinalIgnoreCase))
                    {
                        await DialogHelper.ShowErrorAsync("文件类型错误", "请拖放 .asar 文件");
                        return;
                    }

                    // 确认操作
                    var confirm = await DialogHelper.ShowWarningAsync(
                        "手动替换文件",
                        $"确定要使用 {file.Name} 替换当前的 app.asar 文件吗？\n\n操作前会自动备份原文件。");

                    if (!confirm) return;

                    // 执行替换
                    await ReplaceWithDroppedFileAsync(file);
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync("操作失败", ex.Message);
            }
        }
    }

    private async Task ReplaceWithDroppedFileAsync(Windows.Storage.StorageFile file)
    {
        try
        {
            var detector = App.Services.GetRequiredService<ITermiusDetector>();
            var fileManager = App.Services.GetRequiredService<IFileManager>();
            var processManager = App.Services.GetRequiredService<IProcessManager>();
            var configService = App.Services.GetRequiredService<IConfigService>();

            // 1. 检测 Termius
            var config = await configService.LoadConfigAsync();
            var info = await detector.DetectAsync(config.CustomTermiusPath);

            if (!info.IsFound)
            {
                await DialogHelper.ShowErrorAsync("未找到 Termius", "请在设置中配置 Termius 路径");
                return;
            }

            // 2. 关闭进程
            if (processManager.IsTermiusRunning())
            {
                var closed = await processManager.CloseTermiusAsync(5);
                if (!closed)
                {
                    processManager.KillTermius();
                }
            }

            // 3. 备份
            await fileManager.BackupAsync(info.AsarPath, info.Version);

            // 4. 复制文件
            var tempFile = Path.GetTempFileName();
            await file.CopyAsync(
                Windows.Storage.ApplicationData.Current.TemporaryFolder,
                Path.GetFileName(tempFile),
                Windows.Storage.NameCollisionOption.ReplaceExisting);

            var copiedFile = await Windows.Storage.ApplicationData.Current.TemporaryFolder.GetFileAsync(Path.GetFileName(tempFile));

            // 5. 替换
            await fileManager.ReplaceAsync(copiedFile.Path, info.AsarPath);

            await DialogHelper.ShowRestartPromptAsync();
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("替换失败", ex.Message);
        }
    }
}
