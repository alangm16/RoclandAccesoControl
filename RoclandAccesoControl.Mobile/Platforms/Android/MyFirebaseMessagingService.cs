#if ANDROID
using Android.App;
using Firebase.Messaging;
#endif
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace RoclandAccesoControl.Mobile.Platforms.Android;

/// <summary>
/// Servicio que recibe mensajes FCM tanto en primer plano como en segundo plano.
/// Registra el token cuando Firebase lo renueva y muestra notificaciones locales
/// cuando la app está en primer plano.
/// </summary>

#if ANDROID
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFirebaseMessagingService : FirebaseMessagingService
{
    // Clave donde guardamos el token para que FcmTokenService lo lea
    public const string PrefKey = "fcm_token";

    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);

        // Persistir localmente para que FcmTokenService lo envíe al servidor
        Preferences.Set(PrefKey, token);
        System.Diagnostics.Debug.WriteLine($"[FCM] Nuevo token: {token[..10]}...");
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        // Extraemos título y cuerpo directamente de la Data enviada por el servidor
        string tituloOriginal = "Nueva solicitud";
        if (message.Data.TryGetValue("title", out var titleStr))
            tituloOriginal = titleStr;

        string cuerpo = "";
        if (message.Data.TryGetValue("body", out var bodyStr))
            cuerpo = bodyStr;

        // Agregamos el identificador para nuestra prueba
        string titulo = "🔥 FCM ACTIVO: " + tituloOriginal;

        int notifId = 0;
        if (message.Data.TryGetValue("solicitudId", out var idStr))
            int.TryParse(idStr, out notifId);

        // Mostramos la notificación usando el plugin local
        MostrarNotificacionLocal(notifId, titulo, cuerpo);
    }

    private static void MostrarNotificacionLocal(int id, string titulo, string cuerpo)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var notif = new NotificationRequest
            {
                NotificationId = id == 0 ? new Random().Next(1000, 9999) : id,
                Title = titulo,
                Description = cuerpo,
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
        });
    }
}
#endif
