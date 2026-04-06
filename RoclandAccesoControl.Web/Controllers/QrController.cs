using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace RoclandAccesoControl.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QrController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<QrController> _logger;

    public QrController(IConfiguration config, ILogger<QrController> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve un PNG con el QR apuntando al formulario de acceso.
    /// GET /api/qr/generar?url=https://...
    /// </summary>
    [HttpGet("generar")]
    public IActionResult Generar([FromQuery] string? url = null)
    {
        try
        {
            // Si no se pasa URL, construir desde la request actual
            var targetUrl = url;
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                var req = Request;
                targetUrl = $"{req.Scheme}://{req.Host}/Acceso";
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(targetUrl, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);

            var pngBytes = qrCode.GetGraphic(10);
            return File(pngBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando QR");
            return StatusCode(500, "No se pudo generar el código QR.");
        }
    }

    /// <summary>
    /// Devuelve la URL completa del formulario (útil para mostrar en el panel admin).
    /// GET /api/qr/url
    /// </summary>
    [HttpGet("url")]
    public IActionResult ObtenerUrl()
    {
        var req = Request;
        var url = $"{req.Scheme}://{req.Host}/Acceso";
        return Ok(new { url });
    }
}
