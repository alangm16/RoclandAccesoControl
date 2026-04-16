using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RoclandAccesoControl.Mobile.Models;

namespace RoclandAccesoControl.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly AuthStateService _auth;

    // URL base — se lee desde appsettings o constante de compilación
    private static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? AppConstants.BaseUrlAndroid
            : AppConstants.BaseUrlWindows;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(AuthStateService auth)
    {
        _auth = auth;
        HttpMessageHandler handler;

#if ANDROID
        handler = new Xamarin.Android.Net.AndroidMessageHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
#else
    handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    };
#endif

        _http = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    // ── Auth ───────────────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(string usuario, string password)
    {
        var body = JsonContent.Create(new { usuario, password });
        var resp = await _http.PostAsync("/api/auth/guardia/login", body);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
    }

    // ── Solicitudes ────────────────────────────────────────────────────
    public async Task<List<SolicitudPendiente>> ObtenerSolicitudesAsync()
    {
        SetAuthHeader();
        var resp = await _http.GetAsync("/api/guardias/solicitudes");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<SolicitudPendiente>>(JsonOpts) ?? [];
    }

    // ── Accesos activos ────────────────────────────────────────────────
    public async Task<List<AccesoActivo>> ObtenerActivosAsync()
    {
        SetAuthHeader();
        var resp = await _http.GetAsync("/api/guardias/activos");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<AccesoActivo>>(JsonOpts) ?? [];
    }

    // ── Aprobar ────────────────────────────────────────────────────────
    public async Task<bool> AprobarAsync(AprobarRequest request)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/guardias/aprobar",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    // ── Rechazar ───────────────────────────────────────────────────────
    public async Task<bool> RechazarAsync(RechazarRequest request)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/guardias/rechazar",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<GafeteDisponible>> ObtenerGafetesDisponiblesAsync()
    {
        SetAuthHeader();
        var resp = await _http.GetAsync("/api/guardias/gafetes/disponibles");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<GafeteDisponible>>(JsonOpts) ?? [];
    }

    // ── Marcar salida ──────────────────────────────────────────────────
    public async Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/guardias/salida",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _auth.Token);
    }

    // ── Registrar token FCM ────────────────────────────────────────────
    public async Task<bool> RegistrarFcmTokenAsync(int guardiaId, string fcmToken)
    {
        SetAuthHeader();
        var resp = await _http.PostAsync("/api/guardias/fcm-token",
            new StringContent(
                JsonSerializer.Serialize(new { guardiaId, fcmToken }),
                Encoding.UTF8, "application/json"));
        return resp.IsSuccessStatusCode;
    }

    // ── Obtener Solicitud por ID (Para Deep Linking / Notificaciones) ──
    public async Task<SolicitudPendiente?> ObtenerSolicitudPorIdAsync(int id)
    {
        SetAuthHeader();

        // NOTA: Ajusta la ruta "/api/guardias/solicitud/{id}" si tu endpoint 
        // en el backend (Controller) tiene un nombre diferente.
        var resp = await _http.GetAsync($"/api/guardias/solicitudes/{id}");

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync<SolicitudPendiente>(JsonOpts);
    }
}