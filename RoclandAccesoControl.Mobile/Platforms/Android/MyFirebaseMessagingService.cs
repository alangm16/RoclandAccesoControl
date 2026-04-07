using Android.App;
using Firebase.Messaging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace RoclandAccesoControl.Mobile.Platforms.Android;

/// <summary>
/// Servicio que recibe mensajes FCM tanto en primer plano como en segundo plano.
/// Registra el token cuando Firebase lo renueva y muestra notificaciones locales
/// cuando la app está en primer plano.
/// </summary>

[Service(Exported = false, Name = "com.rocland.accesocontrol.MyFirebaseMessagingService")]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFirebaseMessagingService : FirebaseMessagingService
{
    public const string PrefKey = "fcm_token";

    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        Preferences.Set(PrefKey, token);
        System.Diagnostics.Debug.WriteLine($"[FCM] Nuevo token: {token[..10]}...");
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        // 1. Verificamos si la aplicación está abierta (Primer plano)
        var actividadActual = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (actividadActual != null)
        {
            // La app está abierta. SignalR se está encargando de mostrar la notificación.
            // Firebase se retira en silencio para no duplicar avisos.
            System.Diagnostics.Debug.WriteLine("[FCM] App en primer plano. Ignorando Firebase.");
            return;
        }

        // 2. Si llegamos aquí, la app está minimizada o cerrada. Extraemos la Data.
        string titulo = "Nueva solicitud";
        if (message.Data.TryGetValue("title", out var titleStr))
            titulo = titleStr;

        string cuerpo = "";
        if (message.Data.TryGetValue("body", out var bodyStr))
            cuerpo = bodyStr;

        int notifId = 0;
        if (message.Data.TryGetValue("solicitudId", out var idStr))
            int.TryParse(idStr, out notifId);

        // 3. Mostramos la notificación local
        MostrarNotificacionLocal(notifId, titulo, cuerpo);
    }

    private static void MostrarNotificacionLocal(int id, string titulo, string cuerpo)
    {
        try
        {
            var notif = new NotificationRequest
            {
                NotificationId = id == 0 ? new Random().Next(1000, 9999) : id,
                Title = titulo,
                Description = cuerpo,
                ReturningData = id.ToString(), // ¡CLAVE! Aquí guardamos el ID para la navegación
                BadgeNumber = 1,
                CategoryType = NotificationCategoryType.Status,
                Android =
                {
                    ChannelId = "acceso_control",
                    Priority = AndroidPriority.High,
                    IsGroupSummary = false
                }
            };
            LocalNotificationCenter.Current.Show(notif);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FCM Error Local Notif]: {ex.Message}");
        }
    }
}
