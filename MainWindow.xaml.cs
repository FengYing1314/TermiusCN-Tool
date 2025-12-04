using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TermiusCN_Tool.Helpers;
using TermiusCN_Tool.Views;

namespace TermiusCN_Tool;

/// <summary>
/// 主窗口
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBarDragRegion);

        // 监听 Frame 加载事件以初始化 DialogHelper
        ContentFrame.Loaded += (s, e) =>
        {
            if (ContentFrame.XamlRoot != null)
            {
                DialogHelper.Initialize(ContentFrame.XamlRoot);
            }
        };

        // 默认导航到主页
        NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
        ContentFrame.Navigate(typeof(MainPage));
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag.ToString();
            NavigateToPage(tag);
        }
    }

    private void NavigateToPage(string? tag)
    {
        Type? pageType = tag switch
        {
            "Main" => typeof(MainPage),
            "Backup" => typeof(BackupPage),
            "Settings" => typeof(SettingsPage),
            "Log" => typeof(LogPage),
            "About" => typeof(AboutPage),
            _ => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
