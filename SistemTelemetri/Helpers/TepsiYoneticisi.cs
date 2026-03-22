using System.Windows;
using System.Windows.Forms;

namespace SistemTelemetri.Helpers;

internal sealed class TepsiYoneticisi : IDisposable
{
    private NotifyIcon? _simge;
    private bool _serbestBirakildi;

    public void Baslat(
        Window anaPencere,
        string baslik,
        Action ayarlarAc,
        Action yenile,
        Action cikis,
        Action panelGizleGoster)
    {
        Durdur();

        _simge = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = baslik,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("⚙️ Ayarlar", null, (_, _) => ayarlarAc());
        menu.Items.Add("🔄 Yenile", null, (_, _) => yenile());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("❌ Çıkış", null, (_, _) => cikis());
        _simge.ContextMenuStrip = menu;
        _simge.DoubleClick += (_, _) => panelGizleGoster();
    }

    public void Durdur()
    {
        if (_simge is null)
            return;
        _simge.Visible = false;
        _simge.Dispose();
        _simge = null;
    }

    public void Dispose()
    {
        if (_serbestBirakildi)
            return;
        _serbestBirakildi = true;
        Durdur();
    }
}
