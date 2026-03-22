using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SistemTelemetri.Helpers;

/// <summary>
/// Windows 11 DWM: Mica arka plan, koyu mod ve yuvarlatılmış köşeler.
/// </summary>
internal static class DwmPencereYardimcisi
{
    private const uint DwmwaUseImmersiveDarkMode = 20;
    private const uint DwmwaSystemBackdropType = 38;
    private const uint DwmwaWindowCornerPreference = 33;

    private const int DwmsbtMainWindow = 2;
    private const int DwmwcpRound = 2;

    internal static void MicaVeKoyuModUygula(IntPtr tutamac, bool koyuMod = true)
    {
        if (tutamac == IntPtr.Zero)
            return;

        try
        {
            var koyu = koyuMod ? 1 : 0;
            _ = DwmSetWindowAttribute(tutamac, DwmwaUseImmersiveDarkMode, ref koyu, sizeof(int));

            var mica = DwmsbtMainWindow;
            _ = DwmSetWindowAttribute(tutamac, DwmwaSystemBackdropType, ref mica, sizeof(int));

            var kose = DwmwcpRound;
            _ = DwmSetWindowAttribute(tutamac, DwmwaWindowCornerPreference, ref kose, sizeof(int));
        }
        catch
        {
            // Eski Windows veya DWM yok
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, ref int pvAttribute, int cbAttribute);
}
