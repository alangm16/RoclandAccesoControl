using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes ="AdminCookie", Roles = "Admin,Supervisor")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin) => _admin = admin;

    // ── KPIs ───────────────────────────────────────────────────────────
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis()
        => Ok(await _admin.ObtenerKpisAsync());

    [HttpGet("flujo/horas")]
    public async Task<IActionResult> FlujoPorHora()
        => Ok(await _admin.ObtenerFlujoPorHoraHoyAsync());

    [HttpGet("flujo/diario")]
    public async Task<IActionResult> FlujoDiario([FromQuery] int anio, [FromQuery] int mes)
        => Ok(await _admin.ObtenerFlujoDiarioMesAsync(anio, mes));

    [HttpGet("areas/ranking")]
    public async Task<IActionResult> AreasRanking([FromQuery] int dias = 30)
        => Ok(await _admin.ObtenerAreasMasVisitadasAsync(dias));

    // ── Historial ──────────────────────────────────────────────────────
    [HttpGet("historial")]
    public async Task<IActionResult> Historial(
        [FromQuery] string? busqueda, [FromQuery] string? tipo,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int pagina = 1, [FromQuery] int porPagina = 20)
    {
        var (items, total) = await _admin.ObtenerHistorialAsync(
            busqueda, tipo, desde, hasta, pagina, porPagina);
        return Ok(new { items, total, pagina, porPagina });
    }

    // ── Personas ───────────────────────────────────────────────────────
    [HttpGet("personas/frecuentes")]
    public async Task<IActionResult> PersonasFrecuentes()
        => Ok(await _admin.ObtenerPersonasFrecuentesAsync());

    [HttpGet("personas/{id}")]
    public async Task<IActionResult> PerfilPersona(int id)
    {
        var p = await _admin.ObtenerPerfilPersonaAsync(id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpGet("personas/{id}/historial")]
    public async Task<IActionResult> HistorialPersona(int id)
        => Ok(await _admin.ObtenerHistorialPersonaAsync(id));

    // ── Catálogos ──────────────────────────────────────────────────────
    [HttpPost("areas")]
    public async Task<IActionResult> CrearArea(CatalogoCreateDto dto)
        => Ok(await _admin.CrearAreaAsync(dto));
    [HttpPut("areas/{id}/toggle")]
    public async Task<IActionResult> ToggleArea(int id)
        => Ok(await _admin.ToggleAreaAsync(id));

    [HttpPost("motivos")]
    public async Task<IActionResult> CrearMotivo(CatalogoCreateDto dto)
        => Ok(await _admin.CrearMotivoAsync(dto));
    [HttpPut("motivos/{id}/toggle")]
    public async Task<IActionResult> ToggleMotivo(int id)
        => Ok(await _admin.ToggleMotivoAsync(id));

    [HttpPost("tiposid")]
    public async Task<IActionResult> CrearTipoId(CatalogoCreateDto dto)
        => Ok(await _admin.CrearTipoIdAsync(dto));
    [HttpPut("tiposid/{id}/toggle")]
    public async Task<IActionResult> ToggleTipoId(int id)
        => Ok(await _admin.ToggleTipoIdAsync(id));

    // ── Guardias ───────────────────────────────────────────────────────
    [HttpGet("guardias")]
    public async Task<IActionResult> Guardias()
        => Ok(await _admin.ObtenerGuardiasAsync());
    [HttpPost("guardias")]
    public async Task<IActionResult> CrearGuardia(GuardiaCreateDto dto)
        => Ok(await _admin.CrearGuardiaAsync(dto));
    [HttpPut("guardias/{id}")]
    public async Task<IActionResult> ActualizarGuardia(int id, GuardiaUpdateDto dto)
        => Ok(await _admin.ActualizarGuardiaAsync(id, dto));
    [HttpPut("guardias/{id}/reset")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] string nuevaPassword)
        => Ok(await _admin.ResetPasswordGuardiaAsync(id, nuevaPassword));

    // ── Exportar ───────────────────────────────────────────────────────
    [HttpGet("exportar/excel")]
    public async Task<IActionResult> ExportarExcel()
    {
        var bytes = await _admin.ExportarExcelHoyAsync();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"accesos_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("exportar/pdf")]
    public async Task<IActionResult> ExportarPdf()
    {
        var bytes = await _admin.ExportarPdfHoyAsync();
        return File(bytes, "application/pdf",
            $"accesos_{DateTime.Now:yyyyMMdd}.pdf");
    }
}