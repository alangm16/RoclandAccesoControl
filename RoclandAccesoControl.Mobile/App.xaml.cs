using RoclandAccesoControl.Mobile.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace RoclandAccesoControl.Mobile;

public partial class App : Application
{
    private readonly AuthStateService _auth;
    private string? _idNotificacionPendiente = null;

    public App(AuthStateService auth)
    {
        InitializeComponent();
        _auth = auth;

        MainPage = new AppShell();

        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
    }

    private void OnNotificationTapped(NotificationActionEventArgs e)
    {
        // CHIVATO 1: Si sale esta alerta, Android y el Plugin están funcionando perfecto.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.DisplayAlert("DEBUG 1", $"Toque detectado. Data: '{e.Request.ReturningData}'", "OK");
        });

        if (!string.IsNullOrEmpty(e.Request.ReturningData))
        {
            _idNotificacionPendiente = e.Request.ReturningData;
            NavegarADetalleSiEsPosible();
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            var sesionRestaurada = await _auth.RestaurarSesionAsync();
            await Shell.Current.GoToAsync(sesionRestaurada ? "//Solicitudes" : "//Login");

            NavegarADetalleSiEsPosible();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void NavegarADetalleSiEsPosible()
    {
        if (!string.IsNullOrEmpty(_idNotificacionPendiente))
        {
            var id = _idNotificacionPendiente;
            _idNotificacionPendiente = null;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(600); // Esperamos que cargue la UI

                try
                {
                    // CHIVATO 2: Si sale esta alerta, significa que Shell intentará viajar al Detalle
                    await Shell.Current.DisplayAlert("DEBUG 2", $"Viajando al ID: {id}", "OK");

                    // Aseguramos el enrutamiento con el nombre exacto
                    await Shell.Current.GoToAsync($"DetalleSolicitudPage?id={id}");
                }
                catch (Exception ex)
                {
                    // CHIVATO 3: Si la ruta está mal o hay error en el ViewModel, esto lo atrapará
                    await Shell.Current.DisplayAlert("Error Navegación", ex.Message, "OK");
                }
            });
        }
    }
}