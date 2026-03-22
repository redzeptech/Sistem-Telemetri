using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SistemTelemetri.Helpers;

namespace SistemTelemetri.Converters;

public sealed class YuktenRenkDonusturucu : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
        {
            var g = new SolidColorBrush(RenkBantiYardimcisi.YuzdeyeKarsiRenk(0));
            g.Freeze();
            return g;
        }

        if (value is not double yuzde)
        {
            var g = new SolidColorBrush(RenkBantiYardimcisi.YuzdeyeKarsiRenk(0));
            g.Freeze();
            return g;
        }

        var brush = new SolidColorBrush(RenkBantiYardimcisi.YuzdeyeKarsiRenk(yuzde));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
