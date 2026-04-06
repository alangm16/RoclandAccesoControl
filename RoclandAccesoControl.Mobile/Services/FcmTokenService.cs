#if ANDROID
using Plugin.FirebasePushNotification;
#endif

namespace RoclandAccesoControl.Mobile.Services;

public class FcmTokenService
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    public FcmTokenService(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
    }

    /// <summary>
    /// Obtiene el token FCM actual y lo registra en el servidor.
    /// Llamar después de hacer login.
    /// </summary>
    public async Task RegistrarTokenAsync()
    {
#if ANDROID
        try
        {
            var token = CrossFirebasePushNotification.Current.Token;
            if (!string.IsNullOrEmpty(token))
            {
                await _api.RegistrarFcmTokenAsync(_auth.GuardiaId, token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FCM] Error registrando token: {ex.Message}");
        }
#else
        // Otras plataformas no soportadas
        Console.WriteLine("[FCM] Plataforma no soportada para registro de token.");
#endif
    }
}