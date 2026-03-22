# 🇹🇷 Sistem Telemetri V1.0

**Gelişmiş Donanım İzleme ve Erken Uyarı Sistemi**

*MADE IN TÜRKİYE BY RECEP ŞENEL*

---

## 📊 Sistem Telemetri nedir?

Windows üzerinde donanımınızı canlı izleyen, riskli sıcaklık ve doluluk durumlarında sizi uyararak erken müdahale etmenizi sağlayan yan panel uygulamasıdır.

### İnfografik: Bileşenler

| Bileşen | Özellik (Ne yapar?) | Kullanıcı avantajı (Neden var?) |
|--------|---------------------|----------------------------------|
| 🧠 İşlemci (CPU) | Çekirdek/izlek bazlı yük, sıcaklık; desteklenen işlemcilerde P/E ayrımı | Darboğazı ve aşırı ısınmayı anında fark edin. |
| ⚡ Bellek (RAM) | RAM doluluk, kullanım ve (WMI ile) slot / modül bilgisi | Bellek baskısını ve doluluğu anlık görün. |
| 🎮 Ekran kartı | GPU yük, sıcaklık ve fan devri (sensör mevcutsa) | Oyun ve iş yükünde kart sağlığını koruyun. |
| 💾 Depolama | Çoklu disk okuma/yazma hızı, doluluk ve gecikme | Disk darboğazlarını ve takılmaları yakalayın. |
| 🌐 Ağ paneli | Yerel ve dış IP, ISS, ping, indirme/yükleme hızı | Bağlantı kalitenizi ve adres bilgilerinizi tek bakışta görün. |

---

## 🔥 Neden Sistem Telemetri?

Sıradan izleme araçlarından (ör. SidebarDiagnostics) ayıran başlıca noktalar:

- **Erken uyarı:** İşlemci veya GPU sıcaklığı eşik üzerine çıktığında panel görsel olarak uyarır.
- **Akıllı yan panel:** Pencereyi kaplamaz; masaüstünde yanınıza yerleşir, üstte kalır.
- **IP dedektörü:** Yerel ve dış IP ile ISS bilgisini manuel aramadan görürsünüz.
- **Hafif arayüz:** Veri toplama arka planda; arayüz donmadan güncellenir.

---

## 🛠️ Teknik altyapı

Bu proje **"Safkan Türk"** mühendisliği ve modern kütüphanelerle inşa edildi:

- **C# & WPF** — Windows ile uyumlu masaüstü arayüz.
- **LibreHardwareMonitor** — Donanım sensörlerine erişim.
- **Windows API (AppBar)** — Kenara yaslanan yan çubuk davranışı.
- **Async/await** — Ağır ölçümler arka planda; arayüz akıcı kalır.

---

## 🚀 Kurulum ve kullanım

1. **İndir:** GitHub üzerindeki [Releases](https://github.com/redzeptech/) bölümünden güncel sürümü alın (depo yayınlandığında).
2. **Çalıştır:** Donanım verileri için uygulamayı **Yönetici olarak çalıştırın** (sağ tık).
3. **Özelleştir:** Sağ üstteki ⚙️ ile renk, eşik ve yenileme hızını ayarlayın.
4. **İmza:** En alttaki imzaya tıklayarak geliştirici profiline gidebilirsiniz.

---

## Söz

> *Bu yazılım, bir lise projesi değil; bir sistemin can damarını tutan dijital bir muhafızdır.*

**Geliştirici:** Recep Şenel · [GitHub @redzeptech](https://github.com/redzeptech/)

---

## Derleme

```bash
dotnet build -c Release
```

Sürüm bilgisi `UygulamaBilgisi.cs` ve `SistemTelemetri.csproj` içinde tutulur.
