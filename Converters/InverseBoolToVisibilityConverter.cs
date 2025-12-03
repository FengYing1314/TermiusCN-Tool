using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace TermiusCN_Tool.Converters;

/// <summary>
/// 将 bool 反转并转换为 Visibility 的转换器
/// true -> Collapsed, false -> Visible
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
