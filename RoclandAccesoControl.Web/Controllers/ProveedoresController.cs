using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

// ── Actualización de ProveedoresController con Rate Limiting ─────────
// Reemplaza el archivo existente Controllers/ProveedoresController.cs

[ApiController]
[Route("api/[controller]")]
public class ProveedoresController : ControllerBase
{
    private readonly IAccesoService _acceso;
    public ProveedoresController(IAccesoService acceso) => _acceso = acceso;

    [HttpPost]
    [EnableRateLimiting("FormSubmissionLimit")]
    public async Task<IActionResult> Registrar(
        [FromBody] CrearProveedorRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.ConsentimientoFirmado)
            return BadRequest("El consentimiento es obligatorio.");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _acceso.RegistrarProveedorAsync(request, ip);
        return Ok(result);
    }
}
