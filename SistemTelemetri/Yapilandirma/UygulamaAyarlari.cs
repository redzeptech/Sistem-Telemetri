using CommunityToolkit.Mvvm.ComponentModel;

namespace SistemTelemetri.Yapilandirma;

public partial class UygulamaAyarlari : ObservableObject
{
    [ObservableProperty]
    private double _kenarGenisligi = 300;

    [ObservableProperty]
    private string _arkaPlanRenkHex = "#1A1A1A";

    [ObservableProperty]
    private string _yaziRenkHex = "#FFFFFF";

    [ObservableProperty]
    private double _arkaPlanSaydamligi = 0.85;

    [ObservableProperty]
    private string _metinHizalama = "Sağ";

    [ObservableProperty]
    private double _yaziBoyutu = 14;

    [ObservableProperty]
    private double _uiOlcegi = 1.0;

    [ObservableProperty]
    private double _yatayOfset;

    [ObservableProperty]
    private double _dikeyOfset;

    [ObservableProperty]
    private int _yenilemeMs = 1000;

    [ObservableProperty]
    private bool _tepsiSimgesi;

    [ObservableProperty]
    private bool _baslangictaCalistir;

    [ObservableProperty]
    private bool _gosterZaman = true;

    [ObservableProperty]
    private bool _gosterIslemci = true;

    [ObservableProperty]
    private bool _gosterBellek = true;

    [ObservableProperty]
    private bool _gosterEkranKarti = true;

    [ObservableProperty]
    private bool _gosterDepolama = true;

    [ObservableProperty]
    private bool _gosterAg = true;

    /// <summary>İşlemci sıcaklık uyarı eşiği (°C).</summary>
    [ObservableProperty]
    private double _kritikIslemciSicaklikC = 80;

    /// <summary>GPU sıcaklık uyarı eşiği (°C).</summary>
    [ObservableProperty]
    private double _kritikGpuSicaklikC = 80;

    /// <summary>İşlemci / GPU yük uyarı eşiği (%).</summary>
    [ObservableProperty]
    private double _kritikYukYuzdesi = 95;

    /// <summary>Bellek doluluk uyarı eşiği (%).</summary>
    [ObservableProperty]
    private double _kritikBellekYuzdesi = 92;

    /// <summary>Disk doluluk uyarı eşiği (%).</summary>
    [ObservableProperty]
    private double _kritikDiskDolulukYuzdesi = 95;
}
