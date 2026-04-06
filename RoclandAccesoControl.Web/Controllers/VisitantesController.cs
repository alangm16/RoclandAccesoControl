using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitantesController : ControllerBase
{
    private readonly IAccesoService _acceso;
    public VisitantesController(IAccesoService acceso) => _acceso = acceso;

    [HttpPost]
    [EnableRateLimiting("FormSubmissionLimit")]
    public async Task<IActionResult> Registrar(
        [FromBody] RoclandAccesoControl.Web.Models.DTOs.CrearVisitanteRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.ConsentimientoFirmado)
            return BadRequest("El consentimiento es obligatorio.");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _acceso.RegistrarVisitanteAsync(request, ip);
        return Ok(result);
    }
}
