using System;
using Avalonia.Data.Converters;
using AtomUI.Icons.AntDesign;

namespace OutlineUi.Converters;

public class BoolToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isDark)
        {
            return isDark ? AntDesignIconKind.MoonOutlined : AntDesignIconKind.SunOutlined;
        }
        return AntDesignIconKind.SunOutlined;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}