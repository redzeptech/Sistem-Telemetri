using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace SistemTelemetri.Helpers;

/// <summary>
/// %0–50 soğuk (mavi→yeşil), %51–80 turuncu, %81–100 kırmızı.
/// </summary>
internal static class RenkBantiYardimcisi
{
    internal static MediaColor YuzdeyeKarsiRenk(double yuzde)
    {
        yuzde = Math.Clamp(yuzde, 0, 100);
        if (yuzde <= 50)
        {
            var t = yuzde / 50.0;
            return Lerp(MediaColor.FromRgb(0x09, 0x84, 0xe3), MediaColor.FromRgb(0x00, 0xd2, 0xd3), t);
        }

        if (yuzde <= 80)
        {
            var t = (yuzde - 50) / 30.0;
            return Lerp(MediaColor.FromRgb(0x00, 0xd2, 0xd3), MediaColor.FromRgb(0xf3, 0x9c, 0x12), t);
        }

        {
            var t = (yuzde - 80) / 20.0;
            return Lerp(MediaColor.FromRgb(0xf3, 0x9c, 0x12), MediaColor.FromRgb(0xff, 0x1a, 0x1a), t);
        }
    }

    internal static LinearGradientBrush YatayGradyanCubuk()
    {
        var b = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0.5),
            EndPoint = new System.Windows.Point(1, 0.5),
        };
        b.GradientStops.Add(new GradientStop(MediaColor.FromRgb(0x09, 0x84, 0xe3), 0));
        b.GradientStops.Add(new GradientStop(MediaColor.FromRgb(0x00, 0xd2, 0xd3), 0.35));
        b.GradientStops.Add(new GradientStop(MediaColor.FromRgb(0x00, 0xb8, 0x94), 0.5));
        b.GradientStops.Add(new GradientStop(MediaColor.FromRgb(0xf3, 0x9c, 0x12), 0.8));
        b.GradientStops.Add(new GradientStop(MediaColor.FromRgb(0xff, 0x1a, 0x1a), 1));
        b.Freeze();
        return b;
    }

    private static MediaColor Lerp(MediaColor a, MediaColor b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return MediaColor.FromRgb(
            (byte)Math.Round(a.R + (b.R - a.R) * t),
            (byte)Math.Round(a.G + (b.G - a.G) * t),
            (byte)Math.Round(a.B + (b.B - a.B) * t));
    }
}
