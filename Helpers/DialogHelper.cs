using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TermiusCN_Tool.Helpers;

/// <summary>
/// 对话框辅助类
/// </summary>
public static class DialogHelper
{
    private static XamlRoot? _xamlRoot;

    /// <summary>
    /// 初始化 XamlRoot（在主窗口加载时调用）
    /// </summary>
    public static void Initialize(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    /// <summary>
    /// 显示成功对话框
    /// </summary>
    public static async Task ShowSuccessAsync(string title, string message)
    {
        if (_xamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = _xamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    public static async Task ShowErrorAsync(string title, string message)
    {
        if (_xamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = $"❌ {title}",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = _xamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <returns>用户是否点击了确认</returns>
    public static async Task<bool> ShowConfirmAsync(string title, string message)
    {
        if (_xamlRoot == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "确认",
            CloseButtonText = "取消",
            XamlRoot = _xamlRoot,
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// 显示警告对话框
    /// </summary>
    /// <returns>用户是否点击了继续</returns>
    public static async Task<bool> ShowWarningAsync(string title, string message)
    {
        if (_xamlRoot == null) return false;

        var dialog = new ContentDialog
        {
            Title = $"⚠️ {title}",
            Content = message,
            PrimaryButtonText = "继续",
            CloseButtonText = "取消",
            XamlRoot = _xamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// 显示操作完成后的重启提示
    /// </summary>
    public static async Task ShowRestartPromptAsync()
    {
        if (_xamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = "✅ 汉化完成",
            Content = "文件已成功替换！\n\n请重启 Termius 使汉化生效。\n如需还原，请前往「备份管理」页面。",
            CloseButtonText = "知道了",
            XamlRoot = _xamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        await dialog.ShowAsync();
    }
}
