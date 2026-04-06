using Microsoft.AspNetCore.Mvc;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonasController : ControllerBase
{
    private readonly IAccesoService _acceso;
    public PersonasController(IAccesoService acceso) => _acceso = acceso;

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string numId)
    {
        if (string.IsNullOrWhiteSpace(numId) || numId.Length < 3)
            return BadRequest("Ingresa al menos 3 caracteres.");

        var result = await _acceso.BuscarPersonaAsync(numId);
        return result is null ? NotFound() : Ok(result);
    }
}