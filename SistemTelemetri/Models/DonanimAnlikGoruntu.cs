namespace SistemTelemetri.Models;



public sealed record DonanimAnlikGoruntu(

    string IslemciModelBasligi,

    string IslemciYuku,

    string IslemciSicakligi,

    string EkranKartiYuku,

    string EkranKartiSicakligi,

    string BellekKullanimi,

    string BellekBosAlani,

    string BellekModulDetayMetni,

    string GrafikBirimi,

    string IslemciFanHizMetni,

    string IslemciFanYuzdeMetni,

    string GpuFanHizMetni,

    string GpuFanYuzdeMetni,

    IReadOnlyList<float> CekirdekYukleri,

    IReadOnlyList<CekirdekTuru> CekirdekTurleri,

    /// <summary>Fiziksel disk indeksi → LHM Storage donanım adı.</summary>
    IReadOnlyDictionary<int, string> LhmDepolamaFizikselIndeks,

    double? IslemciYukuYuzdesi,

    double? BellekYukuYuzdesi,

    double? EkranKartiYukuYuzdesi,

    double? IslemciSicaklikDegeri,

    double? GpuSicaklikDegeri);


