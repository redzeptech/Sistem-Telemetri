using System.Collections.Generic;
using System.Linq;
using System.Net;
using SistemTelemetri;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemTelemetri.Services;

/// <summary>
/// Ağ: yerel/dış IP, ISS, ping, trafik akışına göre bağdaştırıcı seçimi ve hız.
/// </summary>
public sealed class AgTelemetriServisi : IDisposable
{
    private const string InternetYokMetni = "🌐 İnternet Erişimi Yok";
    private static readonly TimeSpan HttpOnbellekSuresi = TimeSpan.FromSeconds(75);

    private readonly Ping? _ping;
    private readonly HttpClient _http;
    private readonly Dictionary<string, (long Rx, long Tx)> _sonBayt = new(StringComparer.Ordinal);

    private DateTime _sonHizOrnekZamani = DateTime.UtcNow;
    private bool _ilkHizOrnegi = true;
    private DateTime _sonHttpUtc = DateTime.MinValue;
    private string _onbellekDisIp = InternetYokMetni;
    private string _onbellekIss = "—";
    private bool _serbestBirakildi;

    public AgTelemetriServisi()
    {
        try
        {
            _ping = new Ping();
        }
        catch
        {
            _ping = null;
        }

        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8),
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"SistemTelemetri/{UygulamaBilgisi.Surum}");
    }

    /// <summary>Yerel NIC + hız + ping (Task.Run içinde çalıştırılmalı).</summary>
    public AgOzeti OkuYerelVeHiz(bool pingGonder)
    {
        var simdi = DateTime.UtcNow;
        double indirmeMbs = 0;
        double yuklemeMbs = 0;
        string? yerelIp = null;
        var yerelVar = false;

        YerelVeHizHesapla(simdi, ref indirmeMbs, ref yuklemeMbs, ref yerelIp, ref yerelVar);

        double? pingMs = null;
        if (pingGonder && _ping is not null && yerelVar)
        {
            try
            {
                var yanit = _ping.Send("8.8.8.8", 900);
                if (yanit.Status == IPStatus.Success)
                    pingMs = yanit.RoundtripTime;
            }
            catch
            {
                // yoksay
            }
        }

        return new AgOzeti(
            YerelBaglantiVar: yerelVar,
            PingMs: pingMs,
            IndirmeMbs: indirmeMbs,
            YuklemeMbs: yuklemeMbs,
            YerelIp: yerelIp,
            DisIpGorunumu: _onbellekDisIp,
            IssGorunumu: _onbellekIss);
    }

    public async Task DisIpVeIssGuncelleAsync(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - _sonHttpUtc < HttpOnbellekSuresi)
            return;

        try
        {
            var ip = (await _http.GetStringAsync("https://api.ipify.org", cancellationToken).ConfigureAwait(false))?.Trim();
            if (string.IsNullOrEmpty(ip) || !IPAddress.TryParse(ip, out _))
            {
                _onbellekDisIp = InternetYokMetni;
                _onbellekIss = "—";
            }
            else
            {
                _onbellekDisIp = ip;
                _onbellekIss = await IssAdiniCozAsync(ip, cancellationToken).ConfigureAwait(false) ?? "—";
            }
        }
        catch
        {
            _onbellekDisIp = InternetYokMetni;
            _onbellekIss = "—";
        }

        _sonHttpUtc = DateTime.UtcNow;
    }

    private async Task<string?> IssAdiniCozAsync(string ip, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"http://ip-api.com/json/{Uri.EscapeDataString(ip)}?fields=status,isp";
            var json = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("status", out var st) && st.GetString() == "success" &&
                root.TryGetProperty("isp", out var ispEl))
            {
                var s = ispEl.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s.Trim();
            }
        }
        catch
        {
            // yoksay
        }

        try
        {
            var url = $"https://ipwho.is/{Uri.EscapeDataString(ip)}";
            var json = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("connection", out var conn) &&
                conn.TryGetProperty("isp", out var ispEl))
            {
                var s = ispEl.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s.Trim();
            }
        }
        catch
        {
            // yoksay
        }

        return null;
    }

    public async Task<AgOzeti> OkuAsync(bool pingGonder, CancellationToken cancellationToken = default)
    {
        var yerel = await Task.Run(() => OkuYerelVeHiz(pingGonder), cancellationToken).ConfigureAwait(false);

        try
        {
            await DisIpVeIssGuncelleAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // yoksay
        }

        return yerel with
        {
            DisIpGorunumu = _onbellekDisIp,
            IssGorunumu = _onbellekIss,
        };
    }

    private void YerelVeHizHesapla(
        DateTime simdi,
        ref double indirmeMbs,
        ref double yuklemeMbs,
        ref string? yerelIp,
        ref bool yerelVar)
    {
        var adaylar = AdayArayuzleriTopla();
        if (adaylar.Count == 0)
            return;

        NetworkInterface? secilen = null;
        long enBuyukDelta = -1;

        foreach (var ni in adaylar)
        {
            if (!IstatistikAl(ni, out var rx, out var tx))
                continue;

            _sonBayt.TryGetValue(ni.Id, out var onceki);
            long delta;
            if (_ilkHizOrnegi)
                delta = 0;
            else
            {
                delta = (rx - onceki.Rx) + (tx - onceki.Tx);
                if (delta < 0)
                    delta = 0;
            }

            if (delta > enBuyukDelta)
            {
                enBuyukDelta = delta;
                secilen = ni;
            }
        }

        if (enBuyukDelta <= 0)
            secilen = AdaydanOncelikliSec(adaylar);
        else if (secilen is null)
            secilen = AdaydanOncelikliSec(adaylar);

        yerelIp = YerelIPv4Bul(secilen);
        yerelVar = !string.IsNullOrEmpty(yerelIp);

        var dt = (simdi - _sonHizOrnekZamani).TotalSeconds;
        if (!_ilkHizOrnegi && dt > 0.05 && secilen is not null && IstatistikAl(secilen, out var sRx, out var sTx))
        {
            _sonBayt.TryGetValue(secilen.Id, out var prev);
            indirmeMbs = (sRx - prev.Rx) / dt / (1024.0 * 1024.0);
            yuklemeMbs = (sTx - prev.Tx) / dt / (1024.0 * 1024.0);
            if (indirmeMbs < 0)
                indirmeMbs = 0;
            if (yuklemeMbs < 0)
                yuklemeMbs = 0;
        }

        foreach (var ni in adaylar)
        {
            if (IstatistikAl(ni, out var rx, out var tx))
                _sonBayt[ni.Id] = (rx, tx);
        }

        _sonHizOrnekZamani = simdi;
        _ilkHizOrnegi = false;
    }

    private static List<NetworkInterface> AdayArayuzleriTopla()
    {
        var liste = new List<NetworkInterface>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                try
                {
                    _ = ni.GetIPv4Statistics();
                }
                catch
                {
                    continue;
                }

                var props = ni.GetIPProperties();
                if (!props.UnicastAddresses.Any(u => u.Address.AddressFamily == AddressFamily.InterNetwork))
                    continue;

                liste.Add(ni);
            }
        }
        catch
        {
            // yoksay
        }

        return liste;
    }

    private static bool IstatistikAl(NetworkInterface ni, out long rx, out long tx)
    {
        rx = 0;
        tx = 0;
        try
        {
            var ist = ni.GetIPv4Statistics();
            rx = ist.BytesReceived;
            tx = ist.BytesSent;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static NetworkInterface AdaydanOncelikliSec(List<NetworkInterface> adaylar)
    {
        return adaylar
            .OrderBy(ni => Oncelik(ni.NetworkInterfaceType))
            .ThenBy(ni => ni.Name, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static int Oncelik(NetworkInterfaceType t) => t switch
    {
        NetworkInterfaceType.Ethernet => 0,
        NetworkInterfaceType.GigabitEthernet => 0,
        NetworkInterfaceType.FastEthernetT => 0,
        NetworkInterfaceType.Wireless80211 => 1,
        _ => 2,
    };

    private static string? YerelIPv4Bul(NetworkInterface ni)
    {
        try
        {
            string? yedek = null;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (IPAddress.IsLoopback(ua.Address))
                    continue;
                var s = ua.Address.ToString();
                if (s.StartsWith("169.254.", StringComparison.Ordinal))
                {
                    yedek ??= s;
                    continue;
                }

                return s;
            }

            return yedek;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_serbestBirakildi)
            return;
        _serbestBirakildi = true;
        _ping?.Dispose();
        _http.Dispose();
    }
}

public readonly record struct AgOzeti(
    bool YerelBaglantiVar,
    double? PingMs,
    double IndirmeMbs,
    double YuklemeMbs,
    string? YerelIp,
    string DisIpGorunumu,
    string IssGorunumu);
