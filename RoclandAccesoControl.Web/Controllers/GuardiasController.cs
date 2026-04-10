using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RoclandAccesoControl.Web.Hubs; 
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Guardia")]
public class GuardiasController : ControllerBase
{
    private readonly IAccesoService _acceso;
    private readonly IHubContext<AccesoHub> _hubContext;
    public GuardiasController(IAccesoService acceso, IHubContext<AccesoHub> hubContext)
    {
        _acceso = acceso;
        _hubContext = hubContext;
    }

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

    [HttpGet("gafetes/disponibles")]
    public async Task<IActionResult> ObtenerGafetesDisponibles()
    {
        var result = await _acceso.ObtenerGafetesDisponiblesAsync();
        return Ok(result);
    }

    [HttpPost("aprobar")]
    public async Task<IActionResult> Aprobar(AprobarSolicitudRequest request)
    {
        var ok = await _acceso.AprobarSolicitudAsync(request);
        if (ok)
        {
            // Notificamos a todos que la solicitud ya fue resuelta
            await _hubContext.Clients.Group("Guardias").SendAsync("SolicitudResuelta", new { solicitudId = request.SolicitudId, estado = "Aprobada" });
            return Ok();
        }
        return BadRequest("No se pudo aprobar la solicitud.");
    }

    [HttpPost("rechazar")]
    public async Task<IActionResult> Rechazar(RechazarSolicitudRequest request)
    {
        var ok = await _acceso.RechazarSolicitudAsync(request);
        if (ok)
        {
            // Notificamos la resolución
            await _hubContext.Clients.Group("Guardias").SendAsync("SolicitudResuelta", new { solicitudId = request.SolicitudId, estado = "Rechazada" });
            return Ok();
        }
        return BadRequest("No se pudo rechazar la solicitud.");
    }

    [HttpPost("salida")]
    public async Task<IActionResult> MarcarSalida(MarcarSalidaRequest request)
    {
        var ok = await _acceso.MarcarSalidaAsync(request);
        if (ok)
        {
            // ¡ESTA ES LA CLAVE! Enviamos el evento para que la lista "Dentro ahora" se limpie sola
            await _hubContext.Clients.Group("Guardias").SendAsync("SalidaRegistrada", request.RegistroId);
            return Ok();
        }
        return BadRequest("No se pudo registrar la salida.");
    }

    [HttpPost("fcm-token")]
    public async Task<IActionResult> RegistrarFcmToken([FromBody] RegistrarFcmTokenRequest request)
    {
        var ok = await _acceso.GuardarFcmTokenAsync(request.GuardiaId, request.FcmToken);
        return ok ? Ok() : BadRequest("No se pudo guardar el token.");
    }

    [HttpGet("solicitudes/{id}")]
    public async Task<IActionResult> ObtenerSolicitudPorId(int id)
    {
        // Llamamos al servicio para obtener el detalle de una solicitud específica
        var result = await _acceso.ObtenerSolicitudPorIdAsync(id);

        if (result == null)
        {
            return NotFound("La solicitud no existe, fue cancelada o ya fue procesada.");
        }

        return Ok(result);
    }
}