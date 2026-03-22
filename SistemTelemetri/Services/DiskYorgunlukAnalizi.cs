namespace SistemTelemetri.Services;

/// <summary>
/// Disk aktif zamanı %100'e yakın ve aktarım ani düştüğünde darboğaz tespiti (hafif, halka tampon).
/// </summary>
public sealed class DiskYorgunlukAnalizi
{
    private const int Boyut = 6;
    private readonly double[] _tampon = new double[Boyut];
    private int _yazma;
    private int _sayi;

    /// <summary>
    /// Aktif zaman ~%100 ve toplam okuma+yazma hızı son örneklere göre belirgin düştüyse true.
    /// </summary>
    public bool DarbogazMi(float? diskAktifYuzdesi, float okumaBps, float yazmaBps)
    {
        var toplam = (double)(okumaBps + yazmaBps);
        if (toplam < 0 || double.IsNaN(toplam) || double.IsInfinity(toplam))
            toplam = 0;

        _tampon[_yazma] = toplam;
        var currentIdx = _yazma;
        _yazma = (_yazma + 1) % Boyut;
        if (_sayi < Boyut)
            _sayi++;

        if (diskAktifYuzdesi is not { } aktif || aktif < 99f)
            return false;

        if (_sayi < 4)
            return false;

        var current = _tampon[currentIdx];
        double oncekiToplam = 0;
        var oncekiAdet = 0;
        for (var i = 1; i < _sayi; i++)
        {
            var idx = (currentIdx - i + Boyut) % Boyut;
            oncekiToplam += _tampon[idx];
            oncekiAdet++;
        }

        if (oncekiAdet == 0)
            return false;

        var ortOnceki = oncekiToplam / oncekiAdet;
        if (ortOnceki < 2 * 1024 * 1024)
            return false;

        return current < ortOnceki * 0.18;
    }
}
