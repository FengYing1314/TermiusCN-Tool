using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TermiusCN_Tool.Helpers;
using TermiusCN_Tool.Models;
using TermiusCN_Tool.Services;

namespace TermiusCN_Tool.ViewModels;

/// <summary>
/// 备份管理页面 ViewModel
/// </summary>
public partial class BackupViewModel : ObservableObject
{
    private readonly IFileManager _fileManager;
    private readonly ITermiusDetector _detector;
    private readonly IProcessManager _processManager;
    private readonly IConfigService _configService;

    [ObservableProperty]
    private ObservableCollection<BackupInfo> backups = new();

    [ObservableProperty]
    private BackupInfo? selectedBackup;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasBackups;

    public BackupViewModel(
        IFileManager fileManager,
        ITermiusDetector detector,
        IProcessManager processManager,
        IConfigService configService)
    {
        _fileManager = fileManager;
        _detector = detector;
        _processManager = processManager;
        _configService = configService;
    }

    [RelayCommand]
    private async Task LoadBackups()
    {
        IsLoading = true;
        try
        {
            var backupList = await _fileManager.GetBackupsAsync();
            Backups.Clear();
            foreach (var backup in backupList)
            {
                Backups.Add(backup);
            }
            HasBackups = Backups.Count > 0;
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("加载失败", $"无法加载备份列表: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Restore()
    {
        if (SelectedBackup == null)
        {
            await DialogHelper.ShowErrorAsync("未选择备份", "请先选择要还原的备份");
            return;
        }

        var confirm = await DialogHelper.ShowConfirmAsync(
            "确认还原",
            $"确定要还原备份吗？\n\n备份时间: {SelectedBackup.BackupTimeDisplay}\n版本: {SelectedBackup.Version}\n\n当前文件将被替换为备份文件。");

        if (!confirm) return;

        try
        {
            // 1. 检测 Termius
            var config = await _configService.LoadConfigAsync();
            var info = await _detector.DetectAsync(config.CustomTermiusPath);

            if (!info.IsFound)
            {
                await DialogHelper.ShowErrorAsync("未找到 Termius", "请在设置中配置 Termius 路径");
                return;
            }

            // 2. 关闭进程
            if (_processManager.IsTermiusRunning())
            {
                var closed = await _processManager.CloseTermiusAsync(5);
                if (!closed)
                {
                    var forceKill = await DialogHelper.ShowConfirmAsync(
                        "进程关闭失败",
                        "Termius 进程无法正常关闭，是否强制结束？");

                    if (forceKill)
                    {
                        _processManager.KillTermius();
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // 3. 还原备份
            await _fileManager.RestoreAsync(SelectedBackup, info.AsarPath);

            await DialogHelper.ShowSuccessAsync(
                "还原成功",
                "备份已成功还原！请重启 Termius 使更改生效。");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("还原失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (SelectedBackup == null)
        {
            await DialogHelper.ShowErrorAsync("未选择备份", "请先选择要删除的备份");
            return;
        }

        var confirm = await DialogHelper.ShowConfirmAsync(
            "确认删除",
            $"确定要删除此备份吗？\n\n备份时间: {SelectedBackup.BackupTimeDisplay}\n版本: {SelectedBackup.Version}\n\n此操作不可撤销！");

        if (!confirm) return;

        try
        {
            await _fileManager.DeleteBackupAsync(SelectedBackup);
            Backups.Remove(SelectedBackup);
            HasBackups = Backups.Count > 0;

            await DialogHelper.ShowSuccessAsync("删除成功", "备份已删除");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("删除失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenBackupFolder()
    {
        try
        {
            var backupDir = _fileManager.BackupDirectory;

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // 检查文件夹是否为空，给予用户提示
            var files = Directory.GetFiles(backupDir);
            if (files.Length == 0)
            {
                // 不阻止打开，只是提示一下
                await DialogHelper.ShowWarningAsync("提示", "备份文件夹目前为空。\n\n当您执行「一键汉化」操作时，原版文件会自动备份到此处。");
            }

            // 方法 1: 使用 WinUI 推荐的 Launcher API
            try 
            {
                var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(backupDir);
                var success = await Windows.System.Launcher.LaunchFolderAsync(folder);
                if (success) return;
            }
            catch
            {
                // 忽略 Launcher 错误，尝试降级方案
            }

            // 方法 2: 显式调用 explorer.exe
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{backupDir}\"",
                    UseShellExecute = false // 调用可执行文件时不需要 ShellExecute
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync("打开失败", 
                    $"所有打开方式均失败。\n路径: {backupDir}\n错误: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("系统错误", $"操作失败: {ex.Message}");
        }
    }
}
