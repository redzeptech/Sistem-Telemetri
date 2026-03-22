using System.IO;
using System.Text.Json;

namespace SistemTelemetri.Yapilandirma;

public static class AyarlarDeposu
{
    private static readonly JsonSerializerOptions Secenekler = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string DosyaYolu =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SistemTelemetri",
            "ayarlar.json");

    public static UygulamaAyarlari Yukle()
    {
        try
        {
            if (File.Exists(DosyaYolu))
            {
                var json = File.ReadAllText(DosyaYolu);
                var ayarlar = JsonSerializer.Deserialize<UygulamaAyarlari>(json, Secenekler);
                if (ayarlar is not null)
                    return ayarlar;
            }
        }
        catch
        {
            // yoksay
        }

        return new UygulamaAyarlari();
    }

    public static void Kaydet(UygulamaAyarlari ayarlar)
    {
        try
        {
            var klasor = Path.GetDirectoryName(DosyaYolu);
            if (!string.IsNullOrEmpty(klasor))
                Directory.CreateDirectory(klasor);

            var json = JsonSerializer.Serialize(ayarlar, Secenekler);
            File.WriteAllText(DosyaYolu, json);
        }
        catch
        {
            // yoksay
        }
    }
}
