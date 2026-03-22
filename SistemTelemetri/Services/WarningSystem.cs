using SistemTelemetri.Models;
using SistemTelemetri.Yapilandirma;

namespace SistemTelemetri.Services;

/// <summary>
/// Kullanıcı eşiklerine göre bölüm uyarı durumları (parlama vb.).
/// </summary>
public static class WarningSystem
{
    public static UyariDurumu Degerlendir(
        UygulamaAyarlari ayarlar,
        DonanimAnlikGoruntu g,
        float? diskDolulukYuzdesi)
    {
        var cpuTemp = g.IslemciSicaklikDegeri;
        var gpuTemp = g.GpuSicaklikDegeri;
        var cpuLoad = g.IslemciYukuYuzdesi;
        var gpuLoad = g.EkranKartiYukuYuzdesi;
        var ramLoad = g.BellekYukuYuzdesi;

        var islemciParla =
            (cpuTemp is { } ct && ct >= ayarlar.KritikIslemciSicaklikC) ||
            (cpuLoad is { } cl && cl >= ayarlar.KritikYukYuzdesi);

        var gpuParla =
            (gpuTemp is { } gt && gt >= ayarlar.KritikGpuSicaklikC) ||
            (gpuLoad is { } gl && gl >= ayarlar.KritikYukYuzdesi);

        var bellekParla = ramLoad is { } rl && rl >= ayarlar.KritikBellekYuzdesi;

        var depolamaParla =
            diskDolulukYuzdesi is { } dd && dd >= ayarlar.KritikDiskDolulukYuzdesi;

        return new UyariDurumu(islemciParla, bellekParla, gpuParla, depolamaParla);
    }
}

public readonly record struct UyariDurumu(
    bool IslemciParla,
    bool BellekParla,
    bool GpuParla,
    bool DepolamaParla);
