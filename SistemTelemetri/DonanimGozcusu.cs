using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;
using SistemTelemetri.Models;
using SistemTelemetri.ViewModels;
using SistemTelemetri.Yapilandirma;

namespace SistemTelemetri;

public partial class DonanimGozcusu : ObservableObject, IDisposable
{
    private readonly Computer _bilgisayar;
    private readonly object _kilit = new();
    private bool _serbestBirakildi;
    private readonly string? _wmiIslemciAdi;
    private readonly string? _wmiGpuAdi;

    public DonanimGozcusu()
    {
        _bilgisayar = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
        };
        _bilgisayar.Open();

        _wmiIslemciAdi = WmiIslemciModeliAl();
        _wmiGpuAdi = WmiGpuModeliAl();

        var mantiksal = MantiksalIslemciAdedi();
        for (var i = 0; i < mantiksal; i++)
            CekirdekYukleri.Add(new CekirdekYukuOgesi(i));

        IslemciModelBasligi = !string.IsNullOrWhiteSpace(_wmiIslemciAdi) ? _wmiIslemciAdi! : "İşlemci";
    }

    /// <summary>İzlek başına yük göstergeleri (UI bağlaması).</summary>
    public ObservableCollection<CekirdekYukuOgesi> CekirdekYukleri { get; } = new();

    /// <summary>
    /// Donanım okuma — arka planda çağrılabilir (tek iş parçacığı kilidi).
    /// </summary>
    public DonanimAnlikGoruntu SensorleriOku()
    {
        lock (_kilit)
        {
            var tr = CultureInfo.GetCultureInfo("tr-TR");

            foreach (var hardware in _bilgisayar.Hardware)
                DonanimOgesiniGuncelle(hardware);

            var islemciModelBasligi = !string.IsNullOrWhiteSpace(_wmiIslemciAdi)
                ? _wmiIslemciAdi!
                : "İşlemci";

            var islemciYukuDegeri = IslemciYukunuOku();
            var islemciYukuYuzdesi = islemciYukuDegeri is { } iy ? (double)iy : (double?)null;
            var islemciYuku = islemciYukuDegeri is { } iy2
                ? string.Format(tr, "İşlemci Yükü: {0:F0} %", iy2)
                : "İşlemci Yükü: -- %";

            var islemciSicakligiDegeri = IslemciSicakliginiOku();
            var islemciSicaklikDegeri = islemciSicakligiDegeri is { } isv ? (double)isv : (double?)null;
            var islemciSicakligi = islemciSicakligiDegeri is { } isc
                ? string.Format(tr, "Çekirdek Sıcaklığı: {0:F0} °C", isc)
                : "Çekirdek Sıcaklığı: -- °C";

            var (bellekKullanimi, bellekBosAlani, bellekYukuYuzdesi, bellekModulDetay) = BellekOzetiOku(tr);

            var gpuDonanimi = SeciliEkranKartiDonaniminiAl();
            var gpuAdi = !string.IsNullOrWhiteSpace(_wmiGpuAdi)
                ? _wmiGpuAdi!
                : (!string.IsNullOrWhiteSpace(gpuDonanimi?.Name) ? gpuDonanimi!.Name : null);
            var grafikBirimi = !string.IsNullOrWhiteSpace(gpuAdi)
                ? string.Format(tr, "Grafik Birimi: {0}", gpuAdi)
                : "Grafik Birimi: --";

            var ekranKartiYukuDegeri = EkranKartiYukunuOku(gpuDonanimi);
            var ekranKartiYukuYuzdesi = ekranKartiYukuDegeri is { } eky2 ? (double)eky2 : (double?)null;
            var ekranKartiYuku = ekranKartiYukuDegeri is { } eky
                ? string.Format(tr, "Ekran Kartı Yükü: {0:F0} %", eky)
                : "Ekran Kartı Yükü: -- %";

            var ekranKartiSicakligiDegeri = EkranKartiSicakliginiOku(gpuDonanimi);
            var gpuSicaklikDegeri = ekranKartiSicakligiDegeri is { } gv ? (double)gv : (double?)null;
            var ekranKartiSicakligi = ekranKartiSicakligiDegeri is { } eks
                ? string.Format(tr, "Ekran Kartı Sıcaklığı: {0:F0} °C", eks)
                : "Ekran Kartı Sıcaklığı: -- °C";

            var (islemciFanRpm, islemciFanYuzde) = IslemciFanSensorleriniOku();
            var (gpuFanRpm, gpuFanYuzde) = GpuFanSensorleriniOku(gpuDonanimi);
            var islemciFanHizMetni = FanHizMetniOlustur(tr, islemciFanRpm);
            var islemciFanYuzdeMetni = FanYuzdeMetniOlustur(tr, islemciFanYuzde);
            var gpuFanHizMetni = FanHizMetniOlustur(tr, gpuFanRpm);
            var gpuFanYuzdeMetni = FanYuzdeMetniOlustur(tr, gpuFanYuzde);

            var (cekirdekDizisi, cekirdekTurleri) = CekirdekYukleriniOku(CekirdekYukleri.Count);
            var lhmDepMap = LhmDepolamaIndeksHaritasiOlustur();

            return new DonanimAnlikGoruntu(
                islemciModelBasligi,
                islemciYuku,
                islemciSicakligi,
                ekranKartiYuku,
                ekranKartiSicakligi,
                bellekKullanimi,
                bellekBosAlani,
                bellekModulDetay,
                grafikBirimi,
                islemciFanHizMetni,
                islemciFanYuzdeMetni,
                gpuFanHizMetni,
                gpuFanYuzdeMetni,
                cekirdekDizisi,
                cekirdekTurleri,
                lhmDepMap,
                islemciYukuYuzdesi,
                bellekYukuYuzdesi,
                ekranKartiYukuYuzdesi,
                islemciSicaklikDegeri,
                gpuSicaklikDegeri);
        }
    }

    public void GoruntuUygula(DonanimAnlikGoruntu g, UygulamaAyarlari ayarlar)
    {
        IslemciModelBasligi = g.IslemciModelBasligi;
        IslemciYuku = g.IslemciYuku;
        IslemciYukuYuzdesi = g.IslemciYukuYuzdesi;
        IslemciSicaklikDegeri = g.IslemciSicaklikDegeri;
        IslemciSicakligi = g.IslemciSicakligi;
        IslemciSicaklikUyari = g.IslemciSicaklikDegeri is { } v && v >= ayarlar.KritikIslemciSicaklikC;

        BellekKullanimi = g.BellekKullanimi;
        BellekBosAlani = g.BellekBosAlani;
        BellekYukuYuzdesi = g.BellekYukuYuzdesi;
        BellekModulDetayMetni = g.BellekModulDetayMetni;

        GrafikBirimi = g.GrafikBirimi;
        EkranKartiYuku = g.EkranKartiYuku;
        EkranKartiYukuYuzdesi = g.EkranKartiYukuYuzdesi;
        EkranKartiSicakligi = g.EkranKartiSicakligi;
        GpuSicaklikDegeri = g.GpuSicaklikDegeri;
        GpuSicaklikUyari = g.GpuSicaklikDegeri is { } gv && gv >= ayarlar.KritikGpuSicaklikC;

        IslemciFanHizMetni = g.IslemciFanHizMetni;
        IslemciFanYuzdeMetni = g.IslemciFanYuzdeMetni;
        GpuFanHizMetni = g.GpuFanHizMetni;
        GpuFanYuzdeMetni = g.GpuFanYuzdeMetni;

        var n = Math.Min(CekirdekYukleri.Count, g.CekirdekYukleri.Count);
        for (var i = 0; i < n; i++)
        {
            CekirdekYukleri[i].Yuzde = g.CekirdekYukleri[i];
            if (i < g.CekirdekTurleri.Count)
                CekirdekYukleri[i].Tur = g.CekirdekTurleri[i];
        }
    }

    [ObservableProperty]
    private string _islemciModelBasligi = "İşlemci";

    [ObservableProperty]
    private string _islemciYuku = "İşlemci Yükü: -- %";

    [ObservableProperty]
    private string _islemciSicakligi = "Çekirdek Sıcaklığı: -- °C";

    [ObservableProperty]
    private string _ekranKartiYuku = "Ekran Kartı Yükü: -- %";

    [ObservableProperty]
    private string _ekranKartiSicakligi = "Ekran Kartı Sıcaklığı: -- °C";

    [ObservableProperty]
    private string _bellekKullanimi = "Kullanılan Bellek: -- GB";

    [ObservableProperty]
    private string _bellekBosAlani = "Boş Alan: -- GB";

    [ObservableProperty]
    private string _bellekModulDetayMetni = "Fiziksel bellek: --";

    [ObservableProperty]
    private string _grafikBirimi = "Grafik Birimi: --";

    [ObservableProperty]
    private double? _islemciYukuYuzdesi;

    [ObservableProperty]
    private double? _bellekYukuYuzdesi;

    [ObservableProperty]
    private double? _ekranKartiYukuYuzdesi;

    [ObservableProperty]
    private bool _islemciSicaklikUyari;

    [ObservableProperty]
    private bool _gpuSicaklikUyari;

    [ObservableProperty]
    private double? _islemciSicaklikDegeri;

    [ObservableProperty]
    private double? _gpuSicaklikDegeri;

    [ObservableProperty]
    private string _islemciFanHizMetni = "Fan hızı: ❄️ Pasif";

    [ObservableProperty]
    private string _islemciFanYuzdeMetni = "Fan yüzdesi: -- %";

    [ObservableProperty]
    private string _gpuFanHizMetni = "Fan hızı: ❄️ Pasif";

    [ObservableProperty]
    private string _gpuFanYuzdeMetni = "Fan yüzdesi: -- %";

    private static int MantiksalIslemciAdedi()
    {
        try
        {
            using var sorgu = new ManagementObjectSearcher("SELECT NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (ManagementObject nesne in sorgu.Get())
                return Convert.ToInt32(nesne["NumberOfLogicalProcessors"], CultureInfo.InvariantCulture);
        }
        catch
        {
            // yoksay
        }

        return Environment.ProcessorCount;
    }

    private static string? WmiIslemciModeliAl()
    {
        try
        {
            using var sorgu = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (ManagementObject nesne in sorgu.Get())
                return nesne["Name"]?.ToString()?.Trim();
        }
        catch
        {
            // yoksay
        }

        return null;
    }

    private static string? WmiGpuModeliAl()
    {
        try
        {
            using var sorgu = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
            string? secilen = null;
            ulong enBuyukRam = 0;
            foreach (ManagementObject nesne in sorgu.Get())
            {
                var ad = nesne["Name"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(ad))
                    continue;
                var ram = nesne["AdapterRAM"] is ulong u ? u : 0uL;
                if (ram >= enBuyukRam)
                {
                    enBuyukRam = ram;
                    secilen = ad;
                }
            }

            return secilen;
        }
        catch
        {
            // yoksay
        }

        return null;
    }

    /// <summary>PhysicalDrive indeksi → LHM Storage adı (WMI ile eşleştirilebilir).</summary>
    private IReadOnlyDictionary<int, string> LhmDepolamaIndeksHaritasiOlustur()
    {
        var dict = new Dictionary<int, string>();
        foreach (var hardware in _bilgisayar.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Storage)
                continue;
            var idx = FizikselIndeksLhmDepolama(hardware);
            if (!idx.HasValue || string.IsNullOrWhiteSpace(hardware.Name))
                continue;
            var ad = hardware.Name.Trim();
            if (!dict.TryGetValue(idx.Value, out var mevcut) || ad.Length > mevcut.Length)
                dict[idx.Value] = ad;
        }

        return dict;
    }

    private static int? FizikselIndeksLhmDepolama(IHardware hardware)
    {
        string? kaynak = hardware.Identifier?.ToString();
        if (string.IsNullOrEmpty(kaynak))
            kaynak = hardware.Name;
        if (string.IsNullOrEmpty(kaynak))
            return null;
        var m = Regex.Match(kaynak, @"PhysicalDrive\D*(\d+)", RegexOptions.IgnoreCase);
        if (!m.Success)
            return null;
        return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static CekirdekTuru CekirdekTurunuCikar(string sensorAdi)
    {
        if (sensorAdi.Contains("E-Core", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("E Core", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("Efficiency", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("Gracemont", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("Crestmont", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("Skymont", StringComparison.OrdinalIgnoreCase))
            return CekirdekTuru.Verimlilik;

        if (sensorAdi.Contains("P-Core", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("P Core", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("Raptor Cove", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("Golden Cove", StringComparison.OrdinalIgnoreCase) ||
            sensorAdi.Contains("Redwood Cove", StringComparison.OrdinalIgnoreCase))
            return CekirdekTuru.Performans;

        return CekirdekTuru.Bilinmeyen;
    }

    private (float[] yukler, CekirdekTuru[] turler) CekirdekYukleriniOku(int beklenenAdet)
    {
        if (beklenenAdet <= 0)
            return (Array.Empty<float>(), Array.Empty<CekirdekTuru>());

        var bulunan = new List<(int Indeks, float Yuzde, string Ad)>();
        foreach (var donanim in _bilgisayar.Hardware)
        {
            if (donanim.HardwareType != HardwareType.Cpu)
                continue;

            foreach (var sensor in TumSensorleriGez(donanim))
            {
                if (sensor.SensorType != SensorType.Load || !sensor.Value.HasValue)
                    continue;
                var ad = sensor.Name;
                if (ad.Contains("Total", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!ad.Contains("Core", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!CekirdekIndeksiniCikar(ad, out var idx))
                    continue;
                if (idx < 0 || idx >= beklenenAdet)
                    continue;
                bulunan.Add((idx, sensor.Value.Value, ad));
            }
        }

        bulunan.Sort((a, b) => a.Indeks.CompareTo(b.Indeks));
        var sonuc = new float[beklenenAdet];
        var turler = new CekirdekTuru[beklenenAdet];
        for (var i = 0; i < beklenenAdet; i++)
        {
            var eslesen = bulunan.Where(x => x.Indeks == i).ToList();
            if (eslesen.Count > 0)
            {
                var son = eslesen[^1];
                sonuc[i] = son.Yuzde;
                turler[i] = CekirdekTurunuCikar(son.Ad);
            }
            else
            {
                sonuc[i] = 0f;
                turler[i] = CekirdekTuru.Bilinmeyen;
            }
        }

        return (sonuc, turler);
    }

    private static bool CekirdekIndeksiniCikar(string ad, out int index0Based)
    {
        index0Based = 0;
        var m = Regex.Match(ad, @"(\d+)");
        if (!m.Success)
            return false;
        var n = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        index0Based = n >= 1 ? n - 1 : n;
        return true;
    }

    private static void DonanimOgesiniGuncelle(IHardware donanim)
    {
        donanim.Update();
        foreach (var alt in donanim.SubHardware)
            DonanimOgesiniGuncelle(alt);
    }

    private float? IslemciYukunuOku()
    {
        foreach (var donanim in _bilgisayar.Hardware)
        {
            if (donanim.HardwareType != HardwareType.Cpu)
                continue;

            ISensor? yedek = null;
            foreach (var sensor in TumSensorleriGez(donanim))
            {
                if (sensor.SensorType != SensorType.Load || !sensor.Value.HasValue)
                    continue;
                if (sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                    return sensor.Value.Value;
                yedek ??= sensor;
            }

            if (yedek?.Value is { } y)
                return y;
        }

        return null;
    }

    private float? IslemciSicakliginiOku()
    {
        float? enIyiPaket = null;
        float? enYuksekCekirdek = null;

        foreach (var donanim in _bilgisayar.Hardware)
        {
            if (donanim.HardwareType != HardwareType.Cpu)
                continue;

            foreach (var sensor in TumSensorleriGez(donanim))
            {
                if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue)
                    continue;

                var ad = sensor.Name;
                if (ad.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                    ad.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
                    ad.Contains("Tctl", StringComparison.OrdinalIgnoreCase))
                {
                    var deger = sensor.Value.Value;
                    if (!enIyiPaket.HasValue || deger > enIyiPaket)
                        enIyiPaket = deger;
                }
                else if (ad.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                         ad.Contains("CPU", StringComparison.OrdinalIgnoreCase))
                {
                    var deger = sensor.Value.Value;
                    if (!enYuksekCekirdek.HasValue || deger > enYuksekCekirdek)
                        enYuksekCekirdek = deger;
                }
            }
        }

        if (enIyiPaket.HasValue)
            return enIyiPaket;

        if (enYuksekCekirdek.HasValue)
            return enYuksekCekirdek;

        foreach (var donanim in _bilgisayar.Hardware)
        {
            if (donanim.HardwareType != HardwareType.Cpu)
                continue;
            foreach (var sensor in TumSensorleriGez(donanim))
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                    return sensor.Value.Value;
            }
        }

        return null;
    }

    private IHardware? SeciliEkranKartiDonaniminiAl()
    {
        IHardware? ayrilmis = null;
        IHardware? entegre = null;

        foreach (var donanim in _bilgisayar.Hardware)
        {
            if (donanim.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd)
                ayrilmis ??= donanim;
            else if (donanim.HardwareType == HardwareType.GpuIntel)
                entegre ??= donanim;
        }

        return ayrilmis ?? entegre;
    }

    private float? EkranKartiYukunuOku(IHardware? ekranKarti)
    {
        if (ekranKarti is null)
            return null;

        float? enIyi = null;
        foreach (var sensor in TumSensorleriGez(ekranKarti))
        {
            if (sensor.SensorType != SensorType.Load || !sensor.Value.HasValue)
                continue;
            var ad = sensor.Name;
            if (ad.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                ad.Contains("3D", StringComparison.OrdinalIgnoreCase) ||
                ad.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                ad.Contains("Total", StringComparison.OrdinalIgnoreCase))
            {
                var deger = sensor.Value.Value;
                if (!enIyi.HasValue || deger > enIyi)
                    enIyi = deger;
            }
        }

        if (enIyi.HasValue)
            return enIyi;

        foreach (var sensor in TumSensorleriGez(ekranKarti))
        {
            if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                return sensor.Value.Value;
        }

        return null;
    }

    private float? EkranKartiSicakliginiOku(IHardware? ekranKarti)
    {
        if (ekranKarti is null)
            return null;

        float? enYuksek = null;
        foreach (var sensor in TumSensorleriGez(ekranKarti))
        {
            if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue)
                continue;
            var deger = sensor.Value.Value;
            if (!enYuksek.HasValue || deger > enYuksek)
                enYuksek = deger;
        }

        return enYuksek;
    }

    private (float? rpm, float? yuzde) IslemciFanSensorleriniOku()
    {
        float? maxRpm = null;
        float? maxYuzde = null;

        void FanVeKontrolTopla(IHardware donanim, bool tumFanlar)
        {
            foreach (var sensor in TumSensorleriGez(donanim))
            {
                if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                {
                    var v = sensor.Value.Value;
                    if (tumFanlar || CpuFanIsmineUyuyorMu(sensor.Name))
                    {
                        if (!maxRpm.HasValue || v > maxRpm)
                            maxRpm = v;
                    }
                }

                if (sensor.SensorType == SensorType.Control && sensor.Value.HasValue)
                {
                    var ad = sensor.Name;
                    if (!FanKontrolIsmiGecerliMi(ad))
                        continue;
                    var cv = sensor.Value.Value;
                    if (tumFanlar || CpuFanIsmineUyuyorMu(ad))
                    {
                        if (!maxYuzde.HasValue || cv > maxYuzde)
                            maxYuzde = cv;
                    }
                }
            }
        }

        foreach (var donanim in _bilgisayar.Hardware)
        {
            if (donanim.HardwareType == HardwareType.Cpu)
                FanVeKontrolTopla(donanim, tumFanlar: true);
        }

        if (maxRpm is null && maxYuzde is null)
        {
            foreach (var donanim in _bilgisayar.Hardware)
            {
                if (donanim.HardwareType == HardwareType.Motherboard)
                    FanVeKontrolTopla(donanim, tumFanlar: false);
            }
        }

        return (maxRpm, maxYuzde);
    }

    private static bool CpuFanIsmineUyuyorMu(string ad) =>
        ad.Contains("CPU", StringComparison.OrdinalIgnoreCase) ||
        ad.Contains("CPU Fan", StringComparison.OrdinalIgnoreCase) ||
        ad.Contains("CPU OPT", StringComparison.OrdinalIgnoreCase) ||
        ad.Contains("AIO", StringComparison.OrdinalIgnoreCase) ||
        ad.Contains("Pump", StringComparison.OrdinalIgnoreCase) ||
        ad.Contains("W_PUMP", StringComparison.OrdinalIgnoreCase);

    private static bool FanKontrolIsmiGecerliMi(string ad) =>
        ad.Contains("Fan", StringComparison.OrdinalIgnoreCase) ||
        ad.Contains("Pump", StringComparison.OrdinalIgnoreCase);

    private static (float? rpm, float? yuzde) GpuFanSensorleriniOku(IHardware? ekranKarti)
    {
        if (ekranKarti is null)
            return (null, null);

        float? maxRpm = null;
        float? maxYuzde = null;

        foreach (var sensor in TumSensorleriGez(ekranKarti))
        {
            if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
            {
                var v = sensor.Value.Value;
                if (!maxRpm.HasValue || v > maxRpm)
                    maxRpm = v;
            }

            if (sensor.SensorType == SensorType.Control && sensor.Value.HasValue &&
                FanKontrolIsmiGecerliMi(sensor.Name))
            {
                var cv = sensor.Value.Value;
                if (!maxYuzde.HasValue || cv > maxYuzde)
                    maxYuzde = cv;
            }
        }

        return (maxRpm, maxYuzde);
    }

    private static string FanHizMetniOlustur(CultureInfo tr, float? rpm)
    {
        if (rpm is null or < 1f)
            return "Fan hızı: ❄️ Pasif";
        return string.Format(tr, "Fan hızı: 🌀 Aktif · {0:F0} RPM", rpm.Value);
    }

    private static string FanYuzdeMetniOlustur(CultureInfo tr, float? yuzde)
    {
        if (yuzde is null or < 0.5f)
            return "Fan yüzdesi: -- %";
        return string.Format(tr, "Fan yüzdesi: {0:F0} %", yuzde.Value);
    }

    private static (string Kullanim, string Bos, double? Yuzde, string ModulDetay) BellekOzetiOku(CultureInfo tr)
    {
        var modulDetay = FizikselBellekModulleriniOku(tr);
        try
        {
            using var sorgu = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject nesne in sorgu.Get())
            {
                var toplamKb = Convert.ToUInt64(nesne["TotalVisibleMemorySize"]);
                var bosKb = Convert.ToUInt64(nesne["FreePhysicalMemory"]);
                if (toplamKb == 0)
                    return ("Kullanılan Bellek: -- GB", "Boş Alan: -- GB", null, modulDetay);

                var kullanilanKb = toplamKb - bosKb;
                var kullanilanGb = kullanilanKb / 1024.0 / 1024.0;
                var bosGb = bosKb / 1024.0 / 1024.0;
                var yuzde = kullanilanKb / (double)toplamKb * 100.0;
                return (
                    string.Format(tr, "Kullanılan Bellek: {0:F1} GB", kullanilanGb),
                    string.Format(tr, "Boş Alan: {0:F1} GB", bosGb),
                    yuzde,
                    modulDetay);
            }
        }
        catch
        {
            // yoksay
        }

        return ("Kullanılan Bellek: -- GB", "Boş Alan: -- GB", null, modulDetay);
    }

    private static string FizikselBellekModulleriniOku(CultureInfo tr)
    {
        try
        {
            using var sorgu = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
            ulong toplamBayt = 0;
            var parcalar = new List<string>();
            foreach (ManagementObject nesne in sorgu.Get())
            {
                var cap = Convert.ToUInt64(nesne["Capacity"]);
                toplamBayt += cap;
                var gb = cap / (1024.0 * 1024.0 * 1024.0);
                parcalar.Add(string.Format(tr, "{0:F0} GB", gb));
            }

            if (parcalar.Count == 0)
                return "Fiziksel bellek: —";

            var toplamGb = toplamBayt / (1024.0 * 1024.0 * 1024.0);
            var birlesik = string.Join(" + ", parcalar);
            return string.Format(tr, "Fiziksel: {0:F1} GB toplam ({1})", toplamGb, birlesik);
        }
        catch
        {
            // yoksay
        }

        return "Fiziksel bellek: —";
    }

    private static IEnumerable<ISensor> TumSensorleriGez(IHardware donanim)
    {
        foreach (var sensor in donanim.Sensors)
            yield return sensor;

        foreach (var alt in donanim.SubHardware)
        {
            foreach (var sensor in TumSensorleriGez(alt))
                yield return sensor;
        }
    }

    public void Dispose()
    {
        if (_serbestBirakildi)
            return;
        _serbestBirakildi = true;
        lock (_kilit)
        {
            _bilgisayar.Close();
        }
    }
}
