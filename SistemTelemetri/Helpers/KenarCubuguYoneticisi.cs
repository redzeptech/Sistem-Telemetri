using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SistemTelemetri.Helpers;

internal sealed class KenarCubuguYoneticisi : IDisposable
{
    public bool KayitAktif => _kayitli;

    public int PanelGenisligiPiksel { get; set; } = 300;

    private const uint AbmYeni = 0;
    private const uint AbmKaldir = 1;
    private const uint AbmKonumSorgula = 2;
    private const uint AbmKonumAyarla = 3;

    private const uint SagKenar = 2;

    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;

    private const uint SwpShowWindow = 0x0040;

    private static readonly IntPtr HwndEnUst = new(-1);

    private readonly Window _pencere;
    private readonly IntPtr _tutamac;
    private readonly uint _geriCagriMesaji;

    private HwndSource? _kaynak;
    private HwndSourceHook? _kanca;
    private bool _kayitli;
    private bool _serbestBirakildi;

    public KenarCubuguYoneticisi(Window pencere, IntPtr tutamac)
    {
        _pencere = pencere;
        _tutamac = tutamac;
        _geriCagriMesaji = RegisterWindowMessage("SistemTelemetriKenarCubugu");
    }

    public void Baslat()
    {
        if (_tutamac == IntPtr.Zero || _kayitli)
            return;

        _kaynak = HwndSource.FromHwnd(_tutamac);
        if (_kaynak is null)
            return;

        _kanca = PencereIslemi;
        _kaynak.AddHook(_kanca);

        var veri = new AppBarVerisi
        {
            cbSize = (uint)Marshal.SizeOf<AppBarVerisi>(),
            hWnd = _tutamac,
            uCallbackMessage = _geriCagriMesaji,
            uEdge = 0,
            rc = default,
            lParam = IntPtr.Zero,
        };

        if (SHAppBarMessage(AbmYeni, ref veri) == 0)
        {
            _kaynak.RemoveHook(_kanca);
            _kanca = null;
            return;
        }

        _kayitli = true;
        KenarCubugunuKonumlandir();
    }

    /// <summary>
    /// Panel gizlenirken masaüstü alanını serbest bırakır (AppBar kaldır).
    /// </summary>
    public void AppBarGeciciKaldir()
    {
        if (!_kayitli || _tutamac == IntPtr.Zero)
            return;

        var veri = new AppBarVerisi
        {
            cbSize = (uint)Marshal.SizeOf<AppBarVerisi>(),
            hWnd = _tutamac,
            uCallbackMessage = 0,
            uEdge = 0,
            rc = default,
            lParam = IntPtr.Zero,
        };
        _ = SHAppBarMessage(AbmKaldir, ref veri);
        _kayitli = false;
    }

    /// <summary>
    /// Panel tekrar gösterildiğinde kenar çubuğunu yeniden kaydeder.
    /// </summary>
    public void AppBarGeciciEkle()
    {
        if (_kayitli || _tutamac == IntPtr.Zero)
            return;

        if (_kanca is null)
        {
            _kaynak ??= HwndSource.FromHwnd(_tutamac);
            if (_kaynak is null)
                return;
            _kanca = PencereIslemi;
            _kaynak.AddHook(_kanca);
        }

        var veri = new AppBarVerisi
        {
            cbSize = (uint)Marshal.SizeOf<AppBarVerisi>(),
            hWnd = _tutamac,
            uCallbackMessage = _geriCagriMesaji,
            uEdge = 0,
            rc = default,
            lParam = IntPtr.Zero,
        };

        if (SHAppBarMessage(AbmYeni, ref veri) == 0)
            return;

        _kayitli = true;
        KenarCubugunuKonumlandir();
    }

    public void KenarCubugunuKonumlandir()
    {
        if (!_kayitli || _tutamac == IntPtr.Zero)
            return;

        var ekranGenislik = GetSystemMetrics(SmCxScreen);
        var ekranYukseklik = GetSystemMetrics(SmCyScreen);
        var panelGenisligi = Math.Clamp(PanelGenisligiPiksel, 200, 1200);

        var veri = new AppBarVerisi
        {
            cbSize = (uint)Marshal.SizeOf<AppBarVerisi>(),
            hWnd = _tutamac,
            uCallbackMessage = 0,
            uEdge = SagKenar,
            rc = new RectYapisi
            {
                Left = 0,
                Top = 0,
                Right = ekranGenislik,
                Bottom = ekranYukseklik,
            },
            lParam = IntPtr.Zero,
        };

        _ = SHAppBarMessage(AbmKonumSorgula, ref veri);

        veri.rc.Left = veri.rc.Right - panelGenisligi;

        _ = SHAppBarMessage(AbmKonumAyarla, ref veri);

        var genislik = veri.rc.Right - veri.rc.Left;
        var yukseklik = veri.rc.Bottom - veri.rc.Top;

        _ = SetWindowPos(
            _tutamac,
            HwndEnUst,
            veri.rc.Left,
            veri.rc.Top,
            genislik,
            yukseklik,
            SwpShowWindow);
    }

    private IntPtr PencereIslemi(IntPtr hwnd, int mesaj, IntPtr wParam, IntPtr lParam, ref bool islenmis)
    {
        if (mesaj == _geriCagriMesaji)
        {
            _ = _pencere.Dispatcher.BeginInvoke(new Action(KenarCubugunuKonumlandir));
            islenmis = true;
            return new IntPtr(1);
        }

        return IntPtr.Zero;
    }

    public void Durdur()
    {
        if (_serbestBirakildi)
            return;

        _serbestBirakildi = true;

        if (_kayitli && _tutamac != IntPtr.Zero)
        {
            var veri = new AppBarVerisi
            {
                cbSize = (uint)Marshal.SizeOf<AppBarVerisi>(),
                hWnd = _tutamac,
                uCallbackMessage = 0,
                uEdge = 0,
                rc = default,
                lParam = IntPtr.Zero,
            };
            _ = SHAppBarMessage(AbmKaldir, ref veri);
            _kayitli = false;
        }

        if (_kanca is not null && _kaynak is not null)
        {
            _kaynak.RemoveHook(_kanca);
            _kanca = null;
        }

        _kaynak = null;
    }

    public void Dispose() => Durdur();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("shell32.dll")]
    private static extern uint SHAppBarMessage(uint dwMessage, ref AppBarVerisi pData);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectYapisi
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AppBarVerisi
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RectYapisi rc;
        public IntPtr lParam;
    }
}
