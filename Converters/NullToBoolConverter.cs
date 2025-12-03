using System;
using Microsoft.UI.Xaml.Data;

namespace TermiusCN_Tool.Converters;

/// <summary>
/// 将 null 转换为 bool 的转换器
/// null -> false, 非 null -> true
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is not null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
