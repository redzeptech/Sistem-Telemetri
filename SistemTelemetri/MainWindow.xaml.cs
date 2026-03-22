using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SistemTelemetri.Helpers;
using SistemTelemetri.ViewModels;

namespace SistemTelemetri;

public partial class MainWindow : Window
{
    /// <summary>Ayarlar sütunu açıldığında pencere genişliğine eklenen piksel (canlı veri + ayarlar yan yana).</summary>
    private const double AyarlarPaneliEkGenislik = 300;

    private KenarCubuguYoneticisi? _kenarCubugu;
    private TepsiYoneticisi? _tepsi;
    private EventHandler? _gorselAyarlarOlayi;
    private PropertyChangedEventHandler? _vmDegisti;
    private IntPtr _pencereTutamac;
    private bool _appBarGizlemedenOnceAktifti;
    private HwndSource? _pencereKaynagi;
    private HwndSourceHook? _pencereKancasi;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        _gorselAyarlarOlayi = (_, _) => GorselAyarlariniUygula();
        vm.GorselAyarlarGuncellendi += _gorselAyarlarOlayi;
        GorselAyarlariniUygula();

        _vmDegisti = (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.AyarlarPaneliAcik))
                AyarlarPaneliGenislikAnimasyonu(vm.AyarlarPaneliAcik);
        };
        vm.PropertyChanged += _vmDegisti;
    }

    private static double TemelKenarGenisligi(MainViewModel vm) =>
        Math.Clamp(vm.Ayarlar.KenarGenisligi, 240, 520);

    private double HedefPencereGenisligi(MainViewModel vm) =>
        TemelKenarGenisligi(vm) + (vm.AyarlarPaneliAcik ? AyarlarPaneliEkGenislik : 0);

    private void AyarlarPaneliGenislikAnimasyonu(bool acik)
    {
        if (DataContext is not MainViewModel vm)
            return;

        AyarlarSutunuTanim.Width = acik ? new GridLength(AyarlarPaneliEkGenislik) : new GridLength(0);

        var hedef = HedefPencereGenisligi(vm);
        var baslangic = ActualWidth;
        if (Math.Abs(baslangic - hedef) < 0.5)
        {
            BeginAnimation(WidthProperty, null);
            Width = hedef;
            KenarCubuguGenisliginiSenkronizeEt();
            return;
        }

        var anim = new DoubleAnimation(baslangic, hedef, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        anim.Completed += (_, _) =>
        {
            BeginAnimation(WidthProperty, null);
            Width = hedef;
            KenarCubuguGenisliginiSenkronizeEt();
        };
        BeginAnimation(WidthProperty, anim);
    }

    private void KenarCubuguGenisliginiSenkronizeEt()
    {
        if (_kenarCubugu is null)
        {
            SabitleCalismaAlanina();
            return;
        }

        _kenarCubugu.PanelGenisligiPiksel = (int)Math.Round(ActualWidth);
        if (_kenarCubugu.KayitAktif)
            _kenarCubugu.KenarCubugunuKonumlandir();
        else
            SabitleCalismaAlanina();
    }

    private void AyarlarPaneli_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    /// <summary>
    /// Tepsi / Ctrl+Alt+S: paneli gizle veya göster (AppBar ile uyumlu).
    /// </summary>
    public void PanelGorunurlugunuDegistir()
    {
        if (!IsVisible)
        {
            if (_appBarGizlemedenOnceAktifti)
                _kenarCubugu?.AppBarGeciciEkle();
            else if (_kenarCubugu is { KayitAktif: false })
                SabitleCalismaAlanina();

            Show();
            Topmost = true;
            if (_pencereTutamac != IntPtr.Zero)
                PencereUstZirhYardimcisi.ZirhYenile(_pencereTutamac);
            Activate();
            return;
        }

        _appBarGizlemedenOnceAktifti = _kenarCubugu?.KayitAktif == true;
        if (_appBarGizlemedenOnceAktifti)
            _kenarCubugu?.AppBarGeciciKaldir();
        Hide();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var tutamac = new WindowInteropHelper(this).Handle;
        _pencereTutamac = tutamac;

        DwmPencereYardimcisi.MicaVeKoyuModUygula(tutamac);
        PencereUstZirhYardimcisi.ZirhYenile(tutamac);

        _pencereKaynagi = HwndSource.FromHwnd(tutamac);
        if (_pencereKaynagi is not null)
        {
            _pencereKancasi = PencereMesajKancasi;
            _pencereKaynagi.AddHook(_pencereKancasi);
            _ = KisayolYoneticisi.PanelGizleGosterKaydet(tutamac);
        }

        Deactivated += (_, _) =>
        {
            if (_pencereTutamac != IntPtr.Zero)
                PencereUstZirhYardimcisi.ZirhYenile(_pencereTutamac);
        };

        Activated += (_, _) =>
        {
            if (_pencereTutamac != IntPtr.Zero)
                PencereUstZirhYardimcisi.ZirhYenile(_pencereTutamac);
        };

        _kenarCubugu = new KenarCubuguYoneticisi(this, tutamac);
        _kenarCubugu.Baslat();

        if (DataContext is MainViewModel vm)
        {
            _kenarCubugu.PanelGenisligiPiksel = (int)Math.Round(ActualWidth > 0 ? ActualWidth : HedefPencereGenisligi(vm));
            if (_kenarCubugu.KayitAktif)
                _kenarCubugu.KenarCubugunuKonumlandir();
        }

        if (_kenarCubugu is { KayitAktif: false })
        {
            LocationChanged += OnKonumVeyaBoyutDegisti;
            SizeChanged += OnKonumVeyaBoyutDegisti;
            SabitleCalismaAlanina();
        }
    }

    private IntPtr PencereMesajKancasi(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == KisayolYoneticisi.WmHotkey && wParam.ToInt32() == KisayolYoneticisi.PanelGizleGosterId)
        {
            _ = Dispatcher.BeginInvoke(new Action(PanelGorunurlugunuDegistir));
            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private void OnKonumVeyaBoyutDegisti(object? sender, EventArgs e) => SabitleCalismaAlanina();

    private void SabitleCalismaAlanina()
    {
        var panelGenisligi = DataContext is MainViewModel vm
            ? HedefPencereGenisligi(vm)
            : 300.0;
        var alan = SystemParameters.WorkArea;
        var hedefSol = alan.Left + alan.Width - panelGenisligi;
        var hedefUst = alan.Top;

        if (Math.Abs(Left - hedefSol) > 0.5 ||
            Math.Abs(Top - hedefUst) > 0.5 ||
            Math.Abs(Width - panelGenisligi) > 0.5 ||
            Math.Abs(Height - alan.Height) > 0.5)
        {
            Left = hedefSol;
            Top = hedefUst;
            Width = panelGenisligi;
            Height = alan.Height;
        }
    }

    private void GorselAyarlariniUygula()
    {
        if (DataContext is not MainViewModel vm)
            return;

        BeginAnimation(WidthProperty, null);
        var a = vm.Ayarlar;
        Width = HedefPencereGenisligi(vm);
        AyarlarSutunuTanim.Width = vm.AyarlarPaneliAcik
            ? new GridLength(AyarlarPaneliEkGenislik)
            : new GridLength(0);

        if (_kenarCubugu is not null)
        {
            _kenarCubugu.PanelGenisligiPiksel = (int)Math.Round(Width);
            if (_kenarCubugu.KayitAktif)
                _kenarCubugu.KenarCubugunuKonumlandir();
            else
                SabitleCalismaAlanina();
        }
        else
        {
            SabitleCalismaAlanina();
        }

        try
        {
            var arka = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(a.ArkaPlanRenkHex)!;
            var yazi = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(a.YaziRenkHex)!;
            var alfa = (byte)Math.Round(Math.Clamp(a.ArkaPlanSaydamligi, 0, 1) * 255);
            ArkaPlanKatmani.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(alfa, arka.R, arka.G, arka.B));
            Foreground = new SolidColorBrush(yazi);
        }
        catch
        {
            // yoksay
        }

        IcerikSurukleyici.Padding = new Thickness(
            18 + a.YatayOfset,
            36 + a.DikeyOfset,
            18,
            22);

        if (a.TepsiSimgesi)
        {
            _tepsi ??= new TepsiYoneticisi();
            _tepsi.Baslat(
                this,
                "Sistem Telemetri",
                () => Dispatcher.Invoke(() =>
                {
                    if (DataContext is not MainViewModel m)
                        return;
                    Show();
                    Topmost = true;
                    if (_pencereTutamac != IntPtr.Zero)
                        PencereUstZirhYardimcisi.ZirhYenile(_pencereTutamac);
                    m.AyarlariAcCommand.Execute(null);
                    Activate();
                }),
                () => Dispatcher.Invoke(() =>
                {
                    if (DataContext is not MainViewModel m)
                        return;
                    Show();
                    Topmost = true;
                    if (_pencereTutamac != IntPtr.Zero)
                        PencereUstZirhYardimcisi.ZirhYenile(_pencereTutamac);
                    m.YenileTalebiCommand.Execute(null);
                    Activate();
                }),
                () => Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown()),
                () => Dispatcher.Invoke(PanelGorunurlugunuDegistir));
        }
        else
        {
            _tepsi?.Dispose();
            _tepsi = null;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_pencereTutamac != IntPtr.Zero)
            KisayolYoneticisi.PanelGizleGosterKaldir(_pencereTutamac);

        if (_pencereKancasi is not null && _pencereKaynagi is not null)
        {
            _pencereKaynagi.RemoveHook(_pencereKancasi);
            _pencereKancasi = null;
        }

        _pencereKaynagi = null;

        if (DataContext is MainViewModel vm)
        {
            if (_gorselAyarlarOlayi is not null)
                vm.GorselAyarlarGuncellendi -= _gorselAyarlarOlayi;
            if (_vmDegisti is not null)
                vm.PropertyChanged -= _vmDegisti;
        }

        _tepsi?.Dispose();
        _kenarCubugu?.Dispose();
        if (DataContext is IDisposable d)
            d.Dispose();
    }
}
