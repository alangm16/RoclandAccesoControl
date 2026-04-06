using RoclandAccesoControl.Mobile.Views;

namespace RoclandAccesoControl.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(DetalleSolicitudPage), typeof(DetalleSolicitudPage));
    }
}