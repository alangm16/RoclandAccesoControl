using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Guardia")]
public class GuardiasController : ControllerBase
{
    private readonly IAccesoService _acceso;
    public GuardiasController(IAccesoService acceso) => _acceso = acceso;

    [HttpGet("solicitudes")]
    public async Task<IActionResult> ObtenerSolicitudes()
    {
        var result = await _acceso.ObtenerSolicitudesPendientesAsync();
        return Ok(result);
    }

    [HttpGet("activos")]
    public async Task<IActionResult> ObtenerActivos()
    {
        var result = await _acceso.ObtenerAccesosActivosAsync();
        return Ok(result);
    }

    [HttpPost("aprobar")]
    public async Task<IActionResult> Aprobar(AprobarSolicitudRequest request)
    {
        var ok = await _acceso.AprobarSolicitudAsync(request);
        return ok ? Ok() : BadRequest("No se pudo aprobar la solicitud.");
    }

    [HttpPost("rechazar")]
    public async Task<IActionResult> Rechazar(RechazarSolicitudRequest request)
    {
        var ok = await _acceso.RechazarSolicitudAsync(request);
        return ok ? Ok() : BadRequest("No se pudo rechazar la solicitud.");
    }

    [HttpPost("salida")]
    public async Task<IActionResult> MarcarSalida(MarcarSalidaRequest request)
    {
        var ok = await _acceso.MarcarSalidaAsync(request);
        return ok ? Ok() : BadRequest("No se pudo registrar la salida.");
    }
}