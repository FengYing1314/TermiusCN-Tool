using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TermiusCN_Tool.Helpers;

namespace TermiusCN_Tool.ViewModels;

/// <summary>
/// 日志页面 ViewModel (简化版)
/// </summary>
public partial class LogViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> logs = new();

    [ObservableProperty]
    private string logText = "暂无日志记录\n\n日志功能开发中...";

    public LogViewModel()
    {
        // 添加示例日志
        AddLog("应用程序启动");
        AddLog("初始化完成");
    }

    public void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        Logs.Add(logEntry);
        UpdateLogText();
    }

    private void UpdateLogText()
    {
        LogText = string.Join("\n", Logs);
    }

    [RelayCommand]
    private async Task Clear()
    {
        var confirm = await DialogHelper.ShowConfirmAsync(
            "确认清空",
            "确定要清空所有日志记录吗？");

        if (confirm)
        {
            Logs.Clear();
            LogText = "日志已清空";
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TermiusCN-Tool",
                "Logs");

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(logDir, fileName);

            await File.WriteAllTextAsync(filePath, LogText);

            await DialogHelper.ShowSuccessAsync("导出成功", $"日志已保存到:\n{filePath}");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorAsync("导出失败", ex.Message);
        }
    }
}
