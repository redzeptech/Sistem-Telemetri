using CommunityToolkit.Mvvm.ComponentModel;
using SistemTelemetri.Models;

namespace SistemTelemetri.ViewModels;

public partial class CekirdekYukuOgesi : ObservableObject
{
    public int Indeks { get; }

    [ObservableProperty]
    private double _yuzde;

    [ObservableProperty]
    private CekirdekTuru _tur = CekirdekTuru.Bilinmeyen;

    public string Etiket => Tur switch
    {
        CekirdekTuru.Performans => $"#{Indeks + 1} P",
        CekirdekTuru.Verimlilik => $"#{Indeks + 1} E",
        _ => $"#{Indeks + 1}",
    };

    partial void OnTurChanged(CekirdekTuru value) => OnPropertyChanged(nameof(Etiket));

    public CekirdekYukuOgesi(int indeks) => Indeks = indeks;
}
