using System;
using Microsoft.UI.Xaml.Data;

namespace TermiusCN_Tool.Converters;

/// <summary>
/// 反转布尔值的转换器
/// true -> false, false -> true
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}
