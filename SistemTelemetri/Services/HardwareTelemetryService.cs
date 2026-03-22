using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

using SistemTelemetri.Models;



namespace SistemTelemetri.Services;



/// <summary>

/// Win32 sabit sürücüleri WMI ile listeler; her birim için LogicalDisk sayaçları okur.

/// </summary>

public sealed class HardwareTelemetryService : IDisposable

{

    private readonly object _kilit = new();

    private List<SurucuSayaclari> _suruculer = new();

    private string _surucuImzasi = string.Empty;

    private bool _serbestBirakildi;



    public HardwareTelemetryService()

    {

        SuruculeriYenidenKur();

    }



    public Task<DepolamaTamOzeti> OkuAsync(CancellationToken iptal = default) =>

        Task.Run(() => Oku(), iptal);



    public DepolamaTamOzeti Oku()

    {

        lock (_kilit)

        {

            SuruculeriYenidenKur();

            var liste = new List<DepolamaDiskOzeti>(_suruculer.Count);



            foreach (var s in _suruculer)

            {

                float? okuma = null;

                float? yazma = null;

                float? zaman = null;

                float? oG = null;

                float? yG = null;

                float? doluluk = null;



                try

                {

                    if (s.Okuma is not null)

                        okuma = s.Okuma.NextValue();

                    if (s.Yazma is not null)

                        yazma = s.Yazma.NextValue();

                    if (s.Zaman is not null)

                        zaman = s.Zaman.NextValue();

                    if (s.OkumaGecikme is not null)

                        oG = s.OkumaGecikme.NextValue();

                    if (s.YazmaGecikme is not null)

                        yG = s.YazmaGecikme.NextValue();

                }

                catch

                {

                    // yoksay

                }



                doluluk = SurucuDolulukYuzdesi(s.SurucuHarfi);



                liste.Add(new DepolamaDiskOzeti(

                    s.SurucuHarfi,

                    s.FizikselDiskIndeksi,

                    s.ModelAdi,

                    doluluk,

                    okuma,

                    yazma,

                    zaman,

                    oG,

                    yG));

            }



            return new DepolamaTamOzeti(liste);

        }

    }



    private void SuruculeriYenidenKur()

    {

        var tanimlar = SabitSuruculeriListele();

        var imza = string.Join("|", tanimlar.Select(t => t.Harf));



        if (imza == _surucuImzasi && _suruculer.Count == tanimlar.Count)

            return;



        foreach (var eski in _suruculer)

            eski.Dispose();

        _suruculer = new List<SurucuSayaclari>();



        foreach (var t in tanimlar)

        {

            var ss = new SurucuSayaclari(t.Harf, t.Model ?? t.Harf, t.FizikselIndeks);

            try

            {

                ss.Okuma = new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", t.Harf, true);

                _ = ss.Okuma.NextValue();

                ss.Yazma = new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", t.Harf, true);

                _ = ss.Yazma.NextValue();

            }

            catch

            {

                ss.Dispose();

                continue;

            }



            try

            {

                var z = new PerformanceCounter("LogicalDisk", "% Disk Time", t.Harf, true);

                _ = z.NextValue();

                ss.Zaman = z;

            }

            catch

            {

                // yoksay

            }



            try

            {

                var r = new PerformanceCounter("LogicalDisk", "Avg. Disk sec/Read", t.Harf, true);

                _ = r.NextValue();

                var w = new PerformanceCounter("LogicalDisk", "Avg. Disk sec/Write", t.Harf, true);

                _ = w.NextValue();

                ss.OkumaGecikme = r;

                ss.YazmaGecikme = w;

            }

            catch

            {

                // yoksay

            }



            _suruculer.Add(ss);

        }



        _surucuImzasi = imza;

    }



    private static IReadOnlyList<(string Harf, string? Model, int? FizikselIndeks)> SabitSuruculeriListele()

    {

        var list = new List<(string, string?, int?)>();

        try

        {

            using var sorgu = new ManagementObjectSearcher(

                "SELECT DeviceID, Size, DriveType FROM Win32_LogicalDisk WHERE DriveType=3 AND Size>0");

            foreach (ManagementObject nesne in sorgu.Get())

            {

                var id = nesne["DeviceID"]?.ToString();

                if (string.IsNullOrEmpty(id))

                    continue;

                var (model, fiziksel) = SurucuDiskBilgisiAl(id);

                list.Add((id, model, fiziksel));

            }

        }

        catch

        {

            // yoksay

        }



        list.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));

        return list;

    }



    private static float? SurucuDolulukYuzdesi(string deviceId)

    {

        try

        {

            using var sorgu = new ManagementObjectSearcher(

                $"SELECT Size, FreeSpace FROM Win32_LogicalDisk WHERE DeviceID='{WmiEscape(deviceId)}'");

            foreach (ManagementObject nesne in sorgu.Get())

            {

                var boyut = Convert.ToUInt64(nesne["Size"]);

                var bos = Convert.ToUInt64(nesne["FreeSpace"]);

                if (boyut == 0)

                    return null;

                var kullanilan = boyut - bos;

                return 100f * kullanilan / boyut;

            }

        }

        catch

        {

            // yoksay

        }



        return null;

    }



    private static string WmiEscape(string s) => s.Replace("'", "''");



    /// <summary>

    /// Win32_LogicalDisk → Partition → DiskDrive zinciri ile model ve \\.\PhysicalDriveN indeksi.

    /// </summary>

    private static (string? Model, int? FizikselIndeks) SurucuDiskBilgisiAl(string logicalDeviceId)

    {

        try

        {

            using var s1 = new ManagementObjectSearcher(

                $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{WmiEscape(logicalDeviceId)}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");

            foreach (ManagementObject partition in s1.Get())

            {

                var partId = partition["DeviceID"]?.ToString();

                if (string.IsNullOrEmpty(partId))

                    continue;



                var escaped = WmiEscape(partId);

                using var s2 = new ManagementObjectSearcher(

                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{escaped}'}} WHERE AssocClass=Win32_DiskPartitionToDiskDrive");

                foreach (ManagementObject disk in s2.Get())

                {

                    var m = disk["Model"]?.ToString()?.Trim();

                    int? idx = null;

                    var pnp = disk["PNPDeviceID"]?.ToString();

                    if (!string.IsNullOrEmpty(pnp))

                    {

                        var rm = Regex.Match(pnp, @"PhysicalDrive\D*(\d+)", RegexOptions.IgnoreCase);

                        if (rm.Success)

                            idx = int.Parse(rm.Groups[1].Value, CultureInfo.InvariantCulture);

                    }



                    if (!string.IsNullOrEmpty(m) || idx.HasValue)

                        return (m, idx);

                }

            }

        }

        catch

        {

            // yoksay

        }



        return (null, null);

    }



    public void Dispose()

    {

        if (_serbestBirakildi)

            return;

        _serbestBirakildi = true;

        lock (_kilit)

        {

            foreach (var s in _suruculer)

                s.Dispose();

            _suruculer.Clear();

        }

    }



    private sealed class SurucuSayaclari : IDisposable

    {

        public string SurucuHarfi { get; }

        public int? FizikselDiskIndeksi { get; }

        public string ModelAdi { get; }

        public PerformanceCounter? Okuma { get; set; }

        public PerformanceCounter? Yazma { get; set; }

        public PerformanceCounter? Zaman { get; set; }

        public PerformanceCounter? OkumaGecikme { get; set; }

        public PerformanceCounter? YazmaGecikme { get; set; }



        public SurucuSayaclari(string surucu, string model, int? fizikselDiskIndeksi)

        {

            SurucuHarfi = surucu;

            FizikselDiskIndeksi = fizikselDiskIndeksi;

            ModelAdi = model;

        }



        public void Dispose()

        {

            Okuma?.Dispose();

            Yazma?.Dispose();

            Zaman?.Dispose();

            OkumaGecikme?.Dispose();

            YazmaGecikme?.Dispose();

            Okuma = Yazma = Zaman = OkumaGecikme = YazmaGecikme = null;

        }

    }

}


