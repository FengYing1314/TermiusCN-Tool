using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using TermiusCN_Tool.ViewModels;

namespace TermiusCN_Tool.Views;

/// <summary>
/// 日志页面
/// </summary>
public sealed partial class LogPage : Page
{
    public LogViewModel ViewModel { get; }

    public LogPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<LogViewModel>();
    }
}
