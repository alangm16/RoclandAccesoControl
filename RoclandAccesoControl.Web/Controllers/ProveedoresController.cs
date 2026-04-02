using Microsoft.AspNetCore.Mvc;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProveedoresController : ControllerBase
{
    private readonly IAccesoService _acceso;
    public ProveedoresController(IAccesoService acceso) => _acceso = acceso;

    [HttpPost]
    public async Task<IActionResult> Registrar(CrearProveedorRequest request)
    {
        if (!request.ConsentimientoFirmado)
            return BadRequest("El consentimiento es obligatorio.");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _acceso.RegistrarProveedorAsync(request, ip);
        return Ok(result);
    }
}
