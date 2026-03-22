using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SistemTelemetri.Converters;

public sealed class NullDoubleToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
            return Visibility.Collapsed;
        return value is double ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
