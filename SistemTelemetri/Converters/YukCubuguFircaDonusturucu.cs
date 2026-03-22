using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SistemTelemetri.Helpers;

namespace SistemTelemetri.Converters;

public sealed class YukCubuguFircaDonusturucu : IValueConverter
{
    private static readonly LinearGradientBrush Gradyan = RenkBantiYardimcisi.YatayGradyanCubuk();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
            return Gradyan;
        if (value is not double)
            return Gradyan;

        return Gradyan;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
