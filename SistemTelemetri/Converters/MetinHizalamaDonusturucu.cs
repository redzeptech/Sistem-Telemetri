using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SistemTelemetri.Converters;

public sealed class MetinHizalamaDonusturucu : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == "Sol" ? TextAlignment.Left : TextAlignment.Right;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
