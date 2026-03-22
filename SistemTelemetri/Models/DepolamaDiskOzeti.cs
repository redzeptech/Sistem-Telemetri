namespace SistemTelemetri.Models;



/// <summary>Win32 sabit sürücü + LogicalDisk sayaçlarından tek okuma anlık görüntüsü.</summary>

public readonly record struct DepolamaDiskOzeti(

    string SurucuHarfi,

    /// <summary>\\.\PhysicalDriveN içindeki N; LHM Storage ile eşleme için.</summary>
    int? FizikselDiskIndeksi,

    string ModelAdi,

    float? DolulukYuzdesi,

    float? OkumaBaytSaniye,

    float? YazmaBaytSaniye,

    float? DiskAktifYuzdesi,

    float? OkumaGecikmeSaniye,

    float? YazmaGecikmeSaniye);



/// <summary>Tüm sabit sürücüler için toplu depolama özeti.</summary>

public readonly record struct DepolamaTamOzeti(IReadOnlyList<DepolamaDiskOzeti> Diskler);


