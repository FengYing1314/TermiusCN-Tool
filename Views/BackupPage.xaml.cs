using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TermiusCN_Tool.ViewModels;

namespace TermiusCN_Tool.Views;

/// <summary>
/// 备份管理页面
/// </summary>
public sealed partial class BackupPage : Page
{
    public BackupViewModel ViewModel { get; }

    public BackupPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<BackupViewModel>();
        
        // 订阅属性变更以更新UI
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // 页面加载时触发数据加载
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateEmptyStateVisibility();
        await ViewModel.LoadBackupsCommand.ExecuteAsync(null);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsLoading) || e.PropertyName == nameof(ViewModel.HasBackups))
        {
            UpdateEmptyStateVisibility();
        }
    }

    private void UpdateEmptyStateVisibility()
    {
        // 仅在不加载且没有备份时显示空状态
        EmptyStatePanel.Visibility = (!ViewModel.IsLoading && !ViewModel.HasBackups) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }
}
