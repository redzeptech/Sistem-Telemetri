using System.Globalization;

using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using SistemTelemetri.Helpers;

using SistemTelemetri.Models;

using SistemTelemetri.Services;



namespace SistemTelemetri.ViewModels;



public partial class DiskKartiViewModel : ObservableObject

{

    private readonly CanliSeriTamponu _sparkTamponu = new(36);

    private readonly DiskYorgunlukAnalizi _yorgunluk = new();



    public string SurucuHarf { get; }



    [ObservableProperty]

    private string _baslik;



    [ObservableProperty]

    private double? _dolulukYuzdesi;



    [ObservableProperty]

    private string _dolulukMetni = "Doluluk: -- %";



    [ObservableProperty]

    private string _okumaMetni = "Okuma: -- MB/s";



    [ObservableProperty]

    private string _yazmaMetni = "Yazma: -- MB/s";



    [ObservableProperty]

    private string _aktifZamanMetni = "Aktif zaman: -- %";



    [ObservableProperty]

    private string _gecikmeMetni = "Gecikme: okuma -- ms · yazma -- ms";



    [ObservableProperty]

    private PointCollection _sparkNoktalari = new();



    [ObservableProperty]

    private bool _darbogazUyari;



    public DiskKartiViewModel(string surucuHarf, string baslik)

    {

        SurucuHarf = surucuHarf;

        _baslik = string.IsNullOrWhiteSpace(baslik) ? surucuHarf : baslik;

    }



    public void Guncelle(DepolamaDiskOzeti oz, CultureInfo tr)

    {

        DolulukYuzdesi = oz.DolulukYuzdesi is { } d ? d : null;

        DolulukMetni = oz.DolulukYuzdesi is { } du

            ? string.Format(tr, "Doluluk: {0:F0} %", du)

            : "Doluluk: -- %";



        if (oz.OkumaBaytSaniye is { } rbps && oz.YazmaBaytSaniye is { } wbps)

        {

            var okumaMb = rbps / (1024.0 * 1024.0);

            var yazmaMb = wbps / (1024.0 * 1024.0);

            OkumaMetni = string.Format(tr, "Okuma: {0:F1} MB/s", okumaMb);

            YazmaMetni = string.Format(tr, "Yazma: {0:F1} MB/s", yazmaMb);

        }

        else

        {

            OkumaMetni = "Okuma: -- MB/s";

            YazmaMetni = "Yazma: -- MB/s";

        }



        AktifZamanMetni = oz.DiskAktifYuzdesi is { } aktif

            ? string.Format(tr, "Aktif zaman: {0:F0} %", aktif)

            : "Aktif zaman: -- %";



        if (oz.OkumaGecikmeSaniye is { } ors && oz.YazmaGecikmeSaniye is { } yws)

        {

            var orMs = ors * 1000.0;

            var ywMs = yws * 1000.0;

            GecikmeMetni = string.Format(

                tr,

                "Gecikme: okuma {0:F1} ms · yazma {1:F1} ms",

                orMs,

                ywMs);

        }

        else

        {

            GecikmeMetni = "Gecikme: okuma -- ms · yazma -- ms";

        }



        _sparkTamponu.Ekle(oz.DiskAktifYuzdesi);

        SparkNoktalari = _sparkTamponu.NoktalariOlustur(260, 36);



        var ok = oz.OkumaBaytSaniye ?? 0f;

        var yz = oz.YazmaBaytSaniye ?? 0f;

        DarbogazUyari = _yorgunluk.DarbogazMi(oz.DiskAktifYuzdesi, ok, yz);

    }

}


