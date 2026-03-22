using Microsoft.Win32;

namespace SistemTelemetri.Helpers;

internal static class BaslangicYoneticisi
{
    private const string AnahtarAdi = "SistemTelemetri";

    public static void BaslangictaCalistirAyarla(bool etkin, string? uygulamaYolu = null)
    {
        uygulamaYolu ??= Environment.ProcessPath;
        if (string.IsNullOrEmpty(uygulamaYolu))
            return;

        try
        {
            using var anahtar = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (anahtar is null)
                return;

            if (etkin)
                anahtar.SetValue(AnahtarAdi, $"\"{uygulamaYolu}\"");
            else if (anahtar.GetValue(AnahtarAdi) is not null)
                anahtar.DeleteValue(AnahtarAdi, throwOnMissingValue: false);
        }
        catch
        {
            // yoksay
        }
    }
}
