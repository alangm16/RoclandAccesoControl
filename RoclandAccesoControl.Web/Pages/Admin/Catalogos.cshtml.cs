using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RoclandAccesoControl.Web.Data;
using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class CatalogosModel : PageModel
{
    private readonly RoclandDbContext _db;
    public CatalogosModel(RoclandDbContext db) => _db = db;

    public List<CatalogoItemDto> Areas { get; set; } = [];
    public List<CatalogoItemDto> Motivos { get; set; } = [];
    public List<CatalogoItemDto> TiposId { get; set; } = [];

    public async Task OnGetAsync()
    {
        Areas = await _db.Areas.OrderBy(a => a.Nombre)
            .Select(a => new CatalogoItemDto { Id = a.Id, Nombre = a.Nombre, Activo = a.Activo })
            .ToListAsync();
        Motivos = await _db.MotivosVisita.OrderBy(m => m.Nombre)
            .Select(m => new CatalogoItemDto { Id = m.Id, Nombre = m.Nombre, Activo = m.Activo })
            .ToListAsync();
        TiposId = await _db.TiposIdentificacion.OrderBy(t => t.Nombre)
            .Select(t => new CatalogoItemDto { Id = t.Id, Nombre = t.Nombre, Activo = t.Activo })
            .ToListAsync();
    }
}