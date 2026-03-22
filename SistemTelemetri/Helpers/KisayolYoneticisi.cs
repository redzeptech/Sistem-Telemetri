using System.Runtime.InteropServices;

namespace SistemTelemetri.Helpers;

/// <summary>
/// Ctrl+Alt+S — RegisterHotKey / WM_HOTKEY (işlemci döngüsü yok).
/// </summary>
internal static class KisayolYoneticisi
{
    internal const int PanelGizleGosterId = 0x5354;
    internal const int WmHotkey = 0x0312;

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkS = 0x53;

    internal static bool PanelGizleGosterKaydet(IntPtr tutamac) =>
        RegisterHotKey(tutamac, PanelGizleGosterId, ModControl | ModAlt, VkS);

    internal static void PanelGizleGosterKaldir(IntPtr tutamac)
    {
        if (tutamac != IntPtr.Zero)
            _ = UnregisterHotKey(tutamac, PanelGizleGosterId);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
