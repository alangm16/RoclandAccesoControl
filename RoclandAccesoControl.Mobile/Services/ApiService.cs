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
        var handler = new HttpClientHandler
        {
            // En desarrollo aceptamos certificados autofirmados
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
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
}