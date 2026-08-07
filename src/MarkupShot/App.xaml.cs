namespace MarkupShot;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        DpiAwareness.TryEnablePerMonitorV2();
        base.OnStartup(e);
    }
}
