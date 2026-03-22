using System.Windows.Media;
using MediaPoint = System.Windows.Point;

namespace SistemTelemetri.Helpers;

/// <summary>
/// Mini canlı grafik için halka tampon ve nokta üretimi.
/// </summary>
public sealed class CanliSeriTamponu
{
    private readonly double[] _degerler;
    private int _yazma;
    private int _sayi;

    public CanliSeriTamponu(int kapasite = 48)
    {
        _degerler = new double[kapasite];
    }

    public void Ekle(double? yuzde)
    {
        var v = yuzde ?? 0;
        v = Math.Clamp(v, 0, 100);
        _degerler[_yazma] = v;
        _yazma = (_yazma + 1) % _degerler.Length;
        if (_sayi < _degerler.Length)
            _sayi++;
    }

    public PointCollection NoktalariOlustur(double genislik, double yukseklik)
    {
        if (_sayi < 2 || genislik <= 0 || yukseklik <= 0)
            return new PointCollection();

        var noktalar = new PointCollection(_sayi);
        var adim = genislik / (_sayi - 1);

        var baslangic = _sayi < _degerler.Length
            ? (_yazma - _sayi + _degerler.Length) % _degerler.Length
            : _yazma;

        for (var i = 0; i < _sayi; i++)
        {
            var idx = (baslangic + i) % _degerler.Length;
            var yuzde = _degerler[idx];
            var x = i * adim;
            var y = yukseklik - yuzde / 100.0 * yukseklik;
            noktalar.Add(new MediaPoint(x, y));
        }

        return noktalar;
    }
}
