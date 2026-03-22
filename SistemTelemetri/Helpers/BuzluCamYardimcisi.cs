using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SistemTelemetri.Helpers;

internal static class BuzluCamYardimcisi
{
    internal static void ArkaPlaniBuzluYap(Window pencere)
    {
        pencere.SourceInitialized += (_, _) =>
        {
            var tutamac = new WindowInteropHelper(pencere).Handle;
            if (tutamac == IntPtr.Zero)
                return;

            try
            {
                var politika = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0,
                };

                var boyut = Marshal.SizeOf<AccentPolicy>();
                var bellek = Marshal.AllocHGlobal(boyut);
                try
                {
                    Marshal.StructureToPtr(politika, bellek, false);

                    var veri = new WindowCompositionAttributeData
                    {
                        Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                        Data = bellek,
                        SizeOfData = boyut,
                    };

                    _ = SetWindowCompositionAttribute(tutamac, ref veri);
                }
                finally
                {
                    Marshal.FreeHGlobal(bellek);
                }
            }
            catch
            {
                // Eski Windows sürümlerinde veya kısıtlı ortamlarda yok say.
            }
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    private enum AccentState
    {
        ACCENT_ENABLE_BLURBEHIND = 3,
    }

    private enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
}
