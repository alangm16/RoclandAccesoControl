using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RoclandAccesoControl.Web.Data;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;

namespace RoclandAccesoControl.Web.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly RoclandDbContext _db;
    public LoginModel(RoclandDbContext db) => _db = db;

    [BindProperty] public string Usuario { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Debug: Ver todos los admins activos
    var todos = await _db.Administradores
        .Where(a => a.Activo)
        .Select(a => new { a.Usuario, a.Nombre })
        .ToListAsync();

        // Esto te mostrará en la página qué usuarios existen
        System.Diagnostics.Debug.WriteLine($"Usuarios activos: {string.Join(", ", todos.Select(t => t.Usuario))}");

        var admin = await _db.Administradores
            .FirstOrDefaultAsync(a => a.Usuario == Usuario && a.Activo);

        if (admin is null)
        {
            ErrorMessage = $"Usuario '{Usuario}' no encontrado. Usuarios activos: {string.Join(", ", todos.Select(t => t.Usuario))}";
            return Page();
        }
        //var admin = await _db.Administradores
        //    .FirstOrDefaultAsync(a => a.Usuario == Usuario && a.Activo);

        //if (admin is null || !BCrypt.Net.BCrypt.Verify(Password, admin.PasswordHash))
        //{
        //    ErrorMessage = "Usuario o contraseña incorrectos.";
        //    return Page();
        //}

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Nombre),
            new(ClaimTypes.Role, admin.Rol),
            new("usuario", admin.Usuario)
        };

        var identity = new ClaimsIdentity(claims, "AdminCookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AdminCookie", principal,
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToPage("/Admin/Dashboard");
    }
}