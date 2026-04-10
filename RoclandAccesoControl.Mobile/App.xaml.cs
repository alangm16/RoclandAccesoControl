using RoclandAccesoControl.Mobile.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace RoclandAccesoControl.Mobile;

public partial class App : Application
{
    private readonly AuthStateService _auth;

    // Flag para saber si OnStart ya terminó de navegar a la ruta base.
    // Si llega un tap ANTES de que la shell esté lista, lo guardamos aquí.
    private string? _idNotificacionPendiente = null;
    private bool _shellLista = false;

    public App(AuthStateService auth)
    {
        InitializeComponent();
        _auth = auth;

        MainPage = new AppShell();

        // Suscripción única al evento de tap en notificación
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
    }

    private void OnNotificationTapped(NotificationActionEventArgs e)
    {
        // ReturningData contiene el solicitudId que guardamos al crear la notificación local.
        var data = e.Request?.ReturningData;

        System.Diagnostics.Debug.WriteLine($"[NAV] Notificación tapeada. ReturningData='{data}'");

        if (string.IsNullOrEmpty(data)) return;

        _idNotificacionPendiente = data;

        // Solo intentamos navegar si la shell ya está lista (OnStart terminó).
        // Si no, OnStart llamará a NavegarADetalleSiEsPosible() cuando termine.
        if (_shellLista)
        {
            NavegarADetalle(_idNotificacionPendiente);
            _idNotificacionPendiente = null;
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            var sesionRestaurada = await _auth.RestaurarSesionAsync();
            await Shell.Current.GoToAsync(sesionRestaurada ? "//Bitacora" : "//Login");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            return;
        }

        // La shell ya navegó a su ruta base. A partir de aquí es seguro navegar al detalle.
        _shellLista = true;

        // Si llegó un tap MIENTRAS OnStart estaba trabajando, lo procesamos ahora.
        if (!string.IsNullOrEmpty(_idNotificacionPendiente))
        {
            var id = _idNotificacionPendiente;
            _idNotificacionPendiente = null;
            NavegarADetalle(id);
        }
    }

    private void NavegarADetalle(string solicitudId)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Pequeña pausa para que la UI termine de renderizar la ruta base
                await Task.Delay(400);
                System.Diagnostics.Debug.WriteLine($"[NAV] Navegando a DetalleSolicitudPage?id={solicitudId}");
                await Shell.Current.GoToAsync($"DetalleSolicitudPage?id={solicitudId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NAV Error] {ex.Message}");
                await Shell.Current.DisplayAlert("Error de navegación", ex.Message, "OK");
            }
        });
    }
}