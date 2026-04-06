using Android.App;
using Android.Runtime;
using Plugin.FirebasePushNotification;

namespace RoclandAccesoControl.Mobile
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();

            // También puedes suscribirte al evento de apertura
            CrossFirebasePushNotification.Current.OnNotificationOpened += (s, e) =>
            {
                // Llamamos a un método que definiremos más abajo
                HandlePushNotificationTap(e.Data);
            };
        }

        private void HandlePushNotificationTap(IDictionary<string, object> data)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (data.TryGetValue("solicitudId", out var idObj) && idObj != null)
                {
                    int solicitudId = Convert.ToInt32(idObj);
                    // Navegar al detalle (debes implementar cómo obtener la solicitud)
                    await Shell.Current.GoToAsync($"//Solicitudes/DetalleSolicitudPage?SolicitudId={solicitudId}");
                }
                else
                {
                    // Si no hay ID, solo vamos a la lista
                    await Shell.Current.GoToAsync("//Solicitudes");
                }
            });
        }
    }
}
