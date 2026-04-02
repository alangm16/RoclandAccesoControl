using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RoclandAccesoControl.Web.Data;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Services;

public class AuthService : IAuthService
{
    private readonly RoclandDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(RoclandDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse?> LoginGuardiaAsync(LoginRequest request)
    {
        var guardia = await _db.Guardias
            .FirstOrDefaultAsync(g => g.Usuario == request.Usuario && g.Activo);
        if (guardia is null) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, guardia.PasswordHash))
            return null;

        return GenerarToken(guardia.Id, guardia.Nombre, guardia.Usuario, "Guardia");
    }

    public async Task<LoginResponse?> LoginAdminAsync(LoginRequest request)
    {
        var admin = await _db.Administradores
            .FirstOrDefaultAsync(a => a.Usuario == request.Usuario && a.Activo);
        if (admin is null) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            return null;

        return GenerarToken(admin.Id, admin.Nombre, admin.Usuario, admin.Rol);
    }

    private LoginResponse GenerarToken(int id, string nombre, string usuario, string rol)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiracion = DateTime.UtcNow.AddHours(
            _config.GetValue<int>("Jwt:ExpirationHours"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim(ClaimTypes.Role, rol),
            new Claim("usuario", usuario)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiracion,
            signingCredentials: creds);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            nombre, rol, id, expiracion);
    }
}