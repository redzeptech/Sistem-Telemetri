using System.Runtime.InteropServices;

namespace SistemTelemetri.Helpers;

/// <summary>
/// HWND_TOPMOST ile pencereyi yeniden öne alır (tarayıcı, oyun vb. üstünde kalma).
/// Olay tabanlı: Deactivated / Activated sonrası tek çağrı.
/// </summary>
internal static class PencereUstZirhYardimcisi
{
    private static readonly IntPtr HwndTopmost = new(-1);

    private const uint SwpNomove = 0x0002;
    private const uint SwpNosize = 0x0001;
    private const uint SwpShowwindow = 0x0040;

    internal static void ZirhYenile(IntPtr tutamac)
    {
        if (tutamac == IntPtr.Zero)
            return;
        _ = SetWindowPos(tutamac, HwndTopmost, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpShowwindow);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
