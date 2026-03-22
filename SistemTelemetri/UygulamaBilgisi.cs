namespace SistemTelemetri;

/// <summary>
/// Dağıtım sürümü — tek tanım noktası (derleme sürümü ile uyumlu tutun).
/// </summary>
public static class UygulamaBilgisi
{
    /// <summary>Semantik sürüm (User-Agent, derleme ile eşleşmeli).</summary>
    public const string Surum = "1.0.0";

    public const string GithubProfilUrl = "https://github.com/redzeptech/";

    public const string BaslikBanner =
        "🇹🇷 SİSTEM DURUMU [V1.0.0 FINAL · RECEP ŞENEL]";

    public const string ImzaCubuguMetni =
        "MADE IN TÜRKİYE BY RECEP ŞENEL";

    public const string ProjeTamAdi = "Sistem Telemetri";

    public const string VersiyonEtiketi = "Sürüm 1.0.0 (Final)";

    public const string PencereBasligi = "Sistem Telemetri · V1.0.0 Final";

    public const string HakkındaVurguMetni =
        "BU PROJE RECEP ŞENEL TARAFINDAN GELİŞTİRİLMİŞTİR VE SİZİN SİSTEMİNİZİ KORUMAK İÇİN BURADADIR.";

    /// <summary>Hakkında sekmesi — kısa alt başlık.</summary>
    public const string HakkındaAltBaslik =
        "Gelişmiş Donanım İzleme ve Erken Uyarı Sistemi";

    public const string HakkındaInfografikBaslik =
        "📊 İnfografik: Sistem Telemetri Nedir?";

    public const string HakkındaTabloKolonBilesen = "Bileşen";

    public const string HakkındaTabloKolonOzellik = "Özellik (Ne Yapar?)";

    public const string HakkındaTabloKolonAvantaj = "Kullanıcı Avantajı (Neden Var?)";

    public const string HakkındaTabloIsimCpu = "🧠 İşlemci (CPU)";

    public const string HakkındaTabloIsimRam = "⚡ Bellek (RAM)";

    public const string HakkındaTabloIsimGpu = "🎮 Ekran kartı";

    public const string HakkındaTabloIsimDisk = "💾 Depolama";

    public const string HakkındaTabloIsimAg = "🌐 Ağ paneli";

    public const string HakkındaNedirGiris =
        "Sistem Telemetri, Windows üzerinde donanımınızı canlı izleyen, riskli sıcaklık ve doluluk durumlarında sizi uyararak erken müdahale etmenizi sağlayan yan panel uygulamasıdır.";

    public const string HakkındaTabloCpuOzet =
        "Çekirdek/izlek bazlı yük, sıcaklık; desteklenen işlemcilerde performans/verimlilik çekirdek ayrımı";

    public const string HakkındaTabloCpuAvantaj =
        "Darboğazı ve aşırı ısınmayı anında fark edin.";

    public const string HakkındaTabloRamOzet =
        "RAM doluluk, kullanım ve (WMI ile) slot / modül bilgisi";

    public const string HakkındaTabloRamAvantaj =
        "Bellek baskısını ve doluluğu anlık görün.";

    public const string HakkındaTabloGpuOzet =
        "GPU yük, sıcaklık ve fan devri (sensör mevcutsa)";

    public const string HakkındaTabloGpuAvantaj =
        "Oyun ve iş yükünde kart sağlığını koruyun.";

    public const string HakkındaTabloDiskOzet =
        "Çoklu disk okuma/yazma hızı, doluluk ve gecikme";

    public const string HakkındaTabloDiskAvantaj =
        "Disk darboğazlarını ve takılmaları yakalayın.";

    public const string HakkındaTabloAgOzet =
        "Yerel ve dış IP, ISS, ping, indirme/yükleme hızı";

    public const string HakkındaTabloAgAvantaj =
        "Bağlantı kalitenizi ve adres bilgilerinizi tek bakışta görün.";

    public const string HakkındaNedenBaslik =
        "🔥 Neden \"Sistem Telemetri\"?";

    public const string HakkındaNedenGiris =
        "Sıradan izleme araçlarından (ör. SidebarDiagnostics) ayıran başlıca noktalar:";

    public const string HakkındaNedenMadde1 =
        "• Erken uyarı: İşlemci veya GPU sıcaklığı eşik üzerine çıktığında panel görsel olarak uyarır.";

    public const string HakkındaNedenMadde2 =
        "• Akıllı yan panel: Pencereyi kaplamaz; masaüstünde yanınıza yerleşir, üstte kalır.";

    public const string HakkındaNedenMadde3 =
        "• IP dedektörü: Yerel ve dış IP ile ISS bilgisini manuel aramadan görürsünüz.";

    public const string HakkındaNedenMadde4 =
        "• Hafif arayüz: Veri toplama arka planda; arayüz donmadan güncellenir.";

    public const string HakkındaTeknikBaslik =
        "🛠️ Nasıl yapıldı? (Teknik altyapı)";

    public const string HakkındaTeknikGiris =
        "Bu proje \"Safkan Türk\" mühendisliği ve modern kütüphanelerle inşa edildi:";

    public const string HakkındaTeknikMadde1 = "• C# & WPF — Windows ile uyumlu masaüstü arayüz.";

    public const string HakkındaTeknikMadde2 = "• LibreHardwareMonitor — Donanım sensörlerine erişim.";

    public const string HakkındaTeknikMadde3 = "• Windows API (AppBar) — Kenara yaslanan yan çubuk davranışı.";

    public const string HakkındaTeknikMadde4 = "• Async/await — Ağır ölçümler arka planda; arayüz akıcı kalır.";

    public const string HakkındaKurulumBaslik =
        "🚀 Kurulum ve kullanım";

    public const string HakkındaKurulumMadde1 =
        "1) İndir: GitHub üzerindeki Releases bölümünden güncel sürümü alın.";

    public const string HakkındaKurulumMadde2 =
        "2) Çalıştır: Donanım verileri için uygulamayı Yönetici olarak çalıştırın (sağ tık).";

    public const string HakkındaKurulumMadde3 =
        "3) Özelleştir: Sağ üstteki ⚙️ ile renk, eşik ve yenileme hızını ayarlayın.";

    public const string HakkındaKurulumMadde4 =
        "4) İmza: En alttaki imzaya tıklayarak geliştirici profiline gidebilirsiniz.";

    public const string HakkındaAlinti =
        "“Bu yazılım, bir lise projesi değil; bir sistemin can damarını tutan dijital bir muhafızdır.”";

    public const string HakkındaGelistirici =
        "Geliştirici: Recep Şenel";
}
