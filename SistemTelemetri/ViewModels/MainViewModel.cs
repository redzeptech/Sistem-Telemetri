using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemTelemetri.Helpers;
using SistemTelemetri.Models;
using SistemTelemetri.Services;
using SistemTelemetri.Yapilandirma;

namespace SistemTelemetri.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private const double SicaklikAlarmEsikC = 80;

    private readonly HardwareTelemetryService _depolama;
    private readonly DonanimGozcusu _donanimGozcusu;
    private readonly AgTelemetriServisi _ag;
    private readonly DispatcherTimer _zamanlayici;
    private readonly PropertyChangedEventHandler _ayarlarDegisti;
    private readonly CanliSeriTamponu _cpuSeri = new(48);
    private readonly CanliSeriTamponu _ramSeri = new(48);
    private readonly CanliSeriTamponu _gpuSeri = new(48);
    private readonly CanliSeriTamponu _pingSeri = new(48);
    private readonly CanliSeriTamponu _indirmeSeri = new(48);
    private readonly CanliSeriTamponu _yuklemeSeri = new(48);
    private DateTime _sonPingZamani = DateTime.MinValue;
    private readonly SemaphoreSlim _tickKilidi = new(1, 1);
    private bool _serbestBirakildi;

    public event EventHandler? GorselAyarlarGuncellendi;

    public IReadOnlyList<int> YenilemeSecenekleri { get; } = new[] { 500, 1000, 2000 };

    public IReadOnlyList<string> MetinHizalamaSecenekleri { get; } = new[] { "Sağ", "Sol" };

    public IReadOnlyList<string> RenkOnerileri { get; } = new[]
    {
        "#0D1117", "#121212", "#1A1A1A", "#1E1E2E", "#2B2B2B",
        "#FFFFFF", "#E6E6E6", "#B8E994", "#74B9FF", "#FF7675",
    };

    [ObservableProperty]
    private UygulamaAyarlari _ayarlar;

    [ObservableProperty]
    private bool _ayarlarPaneliAcik;

    [ObservableProperty]
    private string _clockText = string.Empty;

    [ObservableProperty]
    private string _turkishDateText = string.Empty;

    [ObservableProperty]
    private bool _diskDarbogazUyari;

    [ObservableProperty]
    private PointCollection _cpuSparkNoktalari = new();

    [ObservableProperty]
    private PointCollection _ramSparkNoktalari = new();

    [ObservableProperty]
    private PointCollection _gpuSparkNoktalari = new();

    [ObservableProperty]
    private PointCollection _pingSparkNoktalari = new();

    [ObservableProperty]
    private PointCollection _indirmeSparkNoktalari = new();

    [ObservableProperty]
    private PointCollection _yuklemeSparkNoktalari = new();

    [ObservableProperty]
    private bool _islemciBolgesiParliyor;

    [ObservableProperty]
    private bool _bellekBolgesiParliyor;

    [ObservableProperty]
    private bool _gpuBolgesiParliyor;

    [ObservableProperty]
    private bool _depolamaBolgesiParliyor;

    [ObservableProperty]
    private double? _diskDolulukYuzdesi;

    [ObservableProperty]
    private bool _sicaklikAlarmGorunur;

    [ObservableProperty]
    private bool _islemciSicaklikKritik;

    [ObservableProperty]
    private bool _gpuSicaklikKritik;

    [ObservableProperty]
    private bool _agYerelUyariGorunur;

    [ObservableProperty]
    private string _agDisIpSatiri = "🌍 Dış IP: —";

    [ObservableProperty]
    private string _agYerelIpSatiri = "🏠 Yerel IP: —";

    [ObservableProperty]
    private string _agIssSatiri = "📶 Servis: —";

    [ObservableProperty]
    private double? _agPingMs;

    [ObservableProperty]
    private string _agIndirmeMetni = "İndirme: -- MB/s";

    [ObservableProperty]
    private string _agYuklemeMetni = "Yükleme: -- MB/s";

    [ObservableProperty]
    private string _agPingGorunumMetni = "⚡ Ping: —";

    [ObservableProperty]
    private double _indirmeGrafikYuzdesi;

    [ObservableProperty]
    private double _yuklemeGrafikYuzdesi;

    [ObservableProperty]
    private double _pingGrafikYuzdesi;

    /// <summary>Ayarlar çekmecesi: 0 = Ayarlar, 1 = Hakkında.</summary>
    [ObservableProperty]
    private int _ayarlarSekmesiIndex;

    public DonanimGozcusu Donanim => _donanimGozcusu;

    /// <summary>WMI + LogicalDisk ile keşfedilen her sabit sürücü için bir kart.</summary>
    public ObservableCollection<DiskKartiViewModel> DiskKartlari { get; } = new();

    public MainViewModel()
    {
        Ayarlar = AyarlarDeposu.Yukle();
        _ayarlarDegisti = (_, _) => GorselAyarlarGuncellendi?.Invoke(this, EventArgs.Empty);
        Ayarlar.PropertyChanged += _ayarlarDegisti;

        var aralik = TimeSpan.FromMilliseconds(Math.Clamp(Ayarlar.YenilemeMs, 250, 10_000));
        _donanimGozcusu = new DonanimGozcusu();
        _depolama = new HardwareTelemetryService();
        _ag = new AgTelemetriServisi();

        _zamanlayici = new DispatcherTimer { Interval = aralik };
        _zamanlayici.Tick += (_, _) => _ = TickAsync();
        _zamanlayici.Start();

        _ = TickAsync();
    }

    [RelayCommand]
    private void AyarlariAc()
    {
        AyarlarSekmesiIndex = 0;
        AyarlarPaneliAcik = true;
    }

    [RelayCommand]
    private void AyarlariKapat() => AyarlarPaneliAcik = false;

    [RelayCommand]
    private void YenileTalebi() => _ = TickAsync();

    [RelayCommand]
    private void UygulamayiKapat() => System.Windows.Application.Current.Shutdown();

    [RelayCommand]
    private void GithubProfiliniAc()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = UygulamaBilgisi.GithubProfilUrl,
                UseShellExecute = true,
            });
        }
        catch
        {
            // yoksay
        }
    }

    [RelayCommand]
    private void AyarlariKaydet()
    {
        AyarlarDeposu.Kaydet(Ayarlar);
        AyarlarPaneliAcik = false;
        YenilemeAraliginiUygula();
        BaslangicYoneticisi.BaslangictaCalistirAyarla(Ayarlar.BaslangictaCalistir);
        GorselAyarlarGuncellendi?.Invoke(this, EventArgs.Empty);
    }

    private void YenilemeAraliginiUygula()
    {
        var ms = Math.Clamp(Ayarlar.YenilemeMs, 250, 10_000);
        var aralik = TimeSpan.FromMilliseconds(ms);
        _zamanlayici.Stop();
        _zamanlayici.Interval = aralik;
        _zamanlayici.Start();
    }

    private async Task TickAsync()
    {
        if (_serbestBirakildi)
            return;

        try
        {
            var simdi = DateTime.UtcNow;
            var pingIstegi = _sonPingZamani == DateTime.MinValue ||
                             (simdi - _sonPingZamani).TotalSeconds >= 5.0;
            if (pingIstegi)
                _sonPingZamani = simdi;

            var donanimGorev = Task.Run(() => _donanimGozcusu.SensorleriOku());
            var depoGorev = _depolama.OkuAsync();
            var pingArg = pingIstegi;
            var agGorev = _ag.OkuAsync(pingArg);

            await Task.WhenAll(donanimGorev, depoGorev, agGorev).ConfigureAwait(false);

            var g = await donanimGorev;
            var ozet = await depoGorev;
            var ag = await agGorev;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _donanimGozcusu.GoruntuUygula(g, Ayarlar);

                SicaklikAlarminiGuncelle(g);

                var tr = CultureInfo.GetCultureInfo("tr-TR");
                SeriVeUyariGuncelle(g, ozet, ag, pingArg);
                DiskKartlariniGuncelle(tr, ozet, g.LhmDepolamaFizikselIndeks);

                ZamanMetniGuncelle();

                AgMetinleriniGuncelle(tr, ag);
            });
        }
        catch
        {
            // yoksay
        }
        finally
        {
            _tickKilidi.Release();
        }
    }

    private void SicaklikAlarminiGuncelle(DonanimAnlikGoruntu g)
    {
        IslemciSicaklikKritik = g.IslemciSicaklikDegeri is { } c && c > SicaklikAlarmEsikC;
        GpuSicaklikKritik = g.GpuSicaklikDegeri is { } gpu && gpu > SicaklikAlarmEsikC;
        SicaklikAlarmGorunur = IslemciSicaklikKritik || GpuSicaklikKritik;
    }

    private void SeriVeUyariGuncelle(DonanimAnlikGoruntu g, DepolamaTamOzeti ozet, AgOzeti ag, bool pingGuncellendi)
    {
        const double genislik = 280;
        const double yukseklik = 52;

        float? enYuksekDoluluk = null;
        foreach (var d in ozet.Diskler)
        {
            if (d.DolulukYuzdesi is not { } v)
                continue;
            if (!enYuksekDoluluk.HasValue || v > enYuksekDoluluk.Value)
                enYuksekDoluluk = v;
        }
        DiskDolulukYuzdesi = enYuksekDoluluk is { } mx ? (double)mx : null;

        _cpuSeri.Ekle(g.IslemciYukuYuzdesi);
        _ramSeri.Ekle(g.BellekYukuYuzdesi);
        _gpuSeri.Ekle(g.EkranKartiYukuYuzdesi);

        CpuSparkNoktalari = _cpuSeri.NoktalariOlustur(genislik, yukseklik);
        RamSparkNoktalari = _ramSeri.NoktalariOlustur(genislik, yukseklik);
        GpuSparkNoktalari = _gpuSeri.NoktalariOlustur(genislik, yukseklik);

        if (pingGuncellendi)
        {
            if (ag.PingMs is { } p)
            {
                AgPingMs = p;
                AgPingGorunumMetni = string.Format(CultureInfo.GetCultureInfo("tr-TR"), "⚡ Ping: {0:F0} ms", p);
            }
            else
            {
                AgPingMs = null;
                AgPingGorunumMetni = "⚡ Ping: —";
            }
        }

        var pingGrafik = AgPingMs is { } pm ? Math.Min(100, pm) : (double?)null;
        PingGrafikYuzdesi = pingGrafik ?? 0;
        _pingSeri.Ekle(pingGrafik);
        IndirmeGrafikYuzdesi = Math.Min(100, ag.IndirmeMbs * 10);
        YuklemeGrafikYuzdesi = Math.Min(100, ag.YuklemeMbs * 10);
        _indirmeSeri.Ekle(IndirmeGrafikYuzdesi);
        _yuklemeSeri.Ekle(YuklemeGrafikYuzdesi);

        const double agYukseklik = 40;
        PingSparkNoktalari = _pingSeri.NoktalariOlustur(genislik, agYukseklik);
        IndirmeSparkNoktalari = _indirmeSeri.NoktalariOlustur(genislik, agYukseklik);
        YuklemeSparkNoktalari = _yuklemeSeri.NoktalariOlustur(genislik, agYukseklik);

        var uyari = WarningSystem.Degerlendir(Ayarlar, g, enYuksekDoluluk);
        IslemciBolgesiParliyor = uyari.IslemciParla;
        BellekBolgesiParliyor = uyari.BellekParla;
        GpuBolgesiParliyor = uyari.GpuParla;
        DepolamaBolgesiParliyor = uyari.DepolamaParla;
    }

    private void DiskKartlariniGuncelle(CultureInfo tr, DepolamaTamOzeti tam, IReadOnlyDictionary<int, string> lhmMap)
    {
        if (tam.Diskler.Count != DiskKartlari.Count)
        {
            DiskKartlari.Clear();
            foreach (var d in tam.Diskler)
            {
                string baslik;
                if (!string.IsNullOrWhiteSpace(d.ModelAdi) && d.ModelAdi != d.SurucuHarfi)
                    baslik = d.ModelAdi;
                else if (d.FizikselDiskIndeksi is { } fdi && lhmMap.TryGetValue(fdi, out var lhmAd))
                    baslik = lhmAd;
                else
                    baslik = d.SurucuHarfi;
                DiskKartlari.Add(new DiskKartiViewModel(d.SurucuHarfi, baslik));
            }
        }

        for (var i = 0; i < tam.Diskler.Count; i++)
            DiskKartlari[i].Guncelle(tam.Diskler[i], tr);

        DiskDarbogazUyari = DiskKartlari.Any(x => x.DarbogazUyari);
    }

    private void ZamanMetniGuncelle()
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var simdi = DateTime.Now;
        ClockText = simdi.ToString("HH:mm:ss", tr);
        TurkishDateText = simdi.ToString("d MMMM yyyy dddd", tr);
    }

    private void AgMetinleriniGuncelle(CultureInfo tr, AgOzeti ag)
    {
        AgYerelUyariGorunur = !ag.YerelBaglantiVar;
        AgDisIpSatiri = string.Format(tr, "🌍 Dış IP: {0}", ag.DisIpGorunumu);
        AgYerelIpSatiri = string.Format(
            tr,
            "🏠 Yerel IP: {0}",
            string.IsNullOrEmpty(ag.YerelIp) ? "—" : ag.YerelIp);
        AgIssSatiri = string.Format(tr, "📶 Servis: {0}", ag.IssGorunumu);
        AgIndirmeMetni = string.Format(tr, "📥 İndirme: {0:F2} MB/s", ag.IndirmeMbs);
        AgYuklemeMetni = string.Format(tr, "📤 Yükleme: {0:F2} MB/s", ag.YuklemeMbs);
    }

    public void Dispose()
    {
        if (_serbestBirakildi)
            return;
        _serbestBirakildi = true;
        Ayarlar.PropertyChanged -= _ayarlarDegisti;
        _zamanlayici.Stop();
        _tickKilidi.Dispose();
        _donanimGozcusu.Dispose();
        _depolama.Dispose();
        _ag.Dispose();
    }
}
