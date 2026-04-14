using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RoclandAccesoControl.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoclandAccesoControl.Web.Data;

namespace RoclandAccesoControl.Web.Services;

public class FcmService : IFcmService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<FcmService> _logger;
    private readonly IServiceScopeFactory _scopeFactory; // <-- Agregado para la base de datos

    private string FcmEndpoint =>
        $"https://fcm.googleapis.com/v1/projects/{_config["Firebase:ProjectId"]}/messages:send";

    public FcmService(HttpClient http, IConfiguration config, ILogger<FcmService> logger, IServiceScopeFactory scopeFactory)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task EnviarAsync(string deviceToken, string titulo, string cuerpo,
        Dictionary<string, string>? data = null)
    {
        try
        {
            var accessToken = await ObtenerAccessTokenAsync();

            var payloadData = data ?? new Dictionary<string, string>();
            payloadData["title"] = titulo;
            payloadData["body"] = cuerpo;

            var payload = new
            {
                message = new
                {
                    token = deviceToken,
                    data = payloadData,
                    android = new { priority = "HIGH" }
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

                // 👇 RUTINA DE LIMPIEZA AUTOMÁTICA
                if (error.Contains("UNREGISTERED") || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await LimpiarTokenMuertoAsync(deviceToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando push FCM");
        }
    }

    // Nuevo método para borrar el token de la DB
    private async Task LimpiarTokenMuertoAsync(string tokenInvalido)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RoclandDbContext>();

            var guardia = await db.Guardias.FirstOrDefaultAsync(g => g.FcmToken == tokenInvalido);
            if (guardia != null)
            {
                guardia.FcmToken = null;
                await db.SaveChangesAsync();
                _logger.LogInformation("Token obsoleto eliminado exitosamente para el guardia ID: {GuardiaId}", guardia.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar limpiar el token inválido de la base de datos.");
        }
    }

    private async Task<string> ObtenerAccessTokenAsync()
    {
        var serviceAccountJson = _config["Firebase:ServiceAccountJson"]!;
        var serviceAccount = JsonSerializer.Deserialize<JsonElement>(serviceAccountJson);

        var clientEmail = serviceAccount.GetProperty("client_email").GetString()!;
        var privateKey = serviceAccount.GetProperty("private_key").GetString()!
            .Replace("\\n", "\n");

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