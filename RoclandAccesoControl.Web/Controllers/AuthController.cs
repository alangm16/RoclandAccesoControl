using Microsoft.AspNetCore.Mvc;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("guardia/login")]
    public async Task<IActionResult> LoginGuardia(LoginRequest request)
    {
        var result = await _auth.LoginGuardiaAsync(request);
        return result is null ? Unauthorized("Credenciales inválidas") : Ok(result);
    }

    [HttpPost("admin/login")]
    public async Task<IActionResult> LoginAdmin(LoginRequest request)
    {
        var result = await _auth.LoginAdminAsync(request);
        return result is null ? Unauthorized("Credenciales inválidas") : Ok(result);
    }

#if DEBUG
 
    /// Genera un hash BCrypt para insertar en la BD.
    /// Remover en producción o proteger con autenticación.

    [HttpGet("dev/hash")]
    public IActionResult GenerarHash([FromQuery] string pwd)
    {
        if (string.IsNullOrWhiteSpace(pwd)) return BadRequest();
        return Ok(new { hash = BCrypt.Net.BCrypt.HashPassword(pwd) });
    }
#endif
}