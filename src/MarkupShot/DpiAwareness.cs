using System.Runtime.InteropServices;

namespace MarkupShot;

internal static class DpiAwareness
{
    private static readonly nint PerMonitorAwareV2Context = new(-4);

    public static void TryEnablePerMonitorV2()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        SetProcessDpiAwarenessContext(PerMonitorAwareV2Context);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(nint dpiFlag);
}
