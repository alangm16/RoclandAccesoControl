using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Services;

public class FcmService : IFcmService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<FcmService> _logger;

    // FCM v1 API endpoint — reemplaza el API legacy que Google deprecó
    private string FcmEndpoint =>
        $"https://fcm.googleapis.com/v1/projects/{_config["Firebase:ProjectId"]}/messages:send";

    public FcmService(HttpClient http, IConfiguration config, ILogger<FcmService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task EnviarAsync(string deviceToken, string titulo, string cuerpo,
        Dictionary<string, string>? data = null)
    {
        try
        {
            // Obtener access token OAuth2 con la service account key
            var accessToken = await ObtenerAccessTokenAsync();

            var payloadData = data ?? new Dictionary<string, string>();
            payloadData["title"] = titulo;
            payloadData["body"] = cuerpo;

            var payload = new
            {
                message = new
                {
                    token = deviceToken,
                    // 1. ELIMINAMOS EL NODO 'notification' POR COMPLETO

                    // 2. Pasamos todo por el nodo 'data'
                    data = payloadData,

                    android = new
                    {
                        priority = "HIGH"
                        // También eliminamos el bloque de 'notification' dentro de android
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, FcmEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("FCM error para token {Token}: {Error}", deviceToken[..10], error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando push FCM");
        }
    }

    private async Task<string> ObtenerAccessTokenAsync()
    {
        // Leer la service account key JSON desde configuración
        var serviceAccountJson = _config["Firebase:ServiceAccountJson"]!;
        var serviceAccount = JsonSerializer.Deserialize<JsonElement>(serviceAccountJson);

        var clientEmail = serviceAccount.GetProperty("client_email").GetString()!;
        var privateKey = serviceAccount.GetProperty("private_key").GetString()!
            .Replace("\\n", "\n");

        // Crear JWT para autenticar con Google OAuth2
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64UrlEncode(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" }));
        var claims = Base64UrlEncode(JsonSerializer.Serialize(new
        {
            iss = clientEmail,
            sub = clientEmail,
            aud = "https://oauth2.googleapis.com/token",
            iat = now,
            exp = now + 3600,
            scope = "https://www.googleapis.com/auth/firebase.messaging"
        }));

        var signingInput = $"{header}.{claims}";
        var signature = FirmarConRSA(signingInput, privateKey);
        var jwt = $"{signingInput}.{signature}";

        // Intercambiar JWT por access token
        var tokenResp = await _http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion", jwt }
            }));

        var tokenJson = JsonSerializer.Deserialize<JsonElement>(
            await tokenResp.Content.ReadAsStringAsync());
        return tokenJson.GetProperty("access_token").GetString()!;
    }

    private static string FirmarConRSA(string data, string privateKeyPem)
    {
        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var bytes = Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(bytes,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        return Base64UrlEncode(signature);
    }

    private static string Base64UrlEncode(string input) =>
        Base64UrlEncode(Encoding.UTF8.GetBytes(input));

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}