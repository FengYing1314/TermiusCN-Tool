using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using TermiusCN_Tool.ViewModels;

namespace TermiusCN_Tool.Views;

/// <summary>
/// 设置页面
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }
}
