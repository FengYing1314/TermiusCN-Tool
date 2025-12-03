using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace TermiusCN_Tool.Converters;

/// <summary>
/// 将 bool 转换为 Visibility 的转换器
/// true -> Visible, false -> Collapsed
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
