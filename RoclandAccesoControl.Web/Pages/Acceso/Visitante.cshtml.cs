using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RoclandAccesoControl.Web.Data;
using RoclandAccesoControl.Web.Models.DTOs;

namespace RoclandAccesoControl.Web.Pages.Acceso;

public class VisitanteModel : PageModel
{
    private readonly RoclandDbContext _db;

    public VisitanteModel(RoclandDbContext db) => _db = db;

    public List<CatalogoItemDto> TiposIdentificacion { get; set; } = new();
    public List<CatalogoItemDto> Areas { get; set; } = new();
    public List<CatalogoItemDto> Motivos { get; set; } = new();

    public async Task OnGetAsync()
    {
        TiposIdentificacion = await _db.TiposIdentificacion
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .Select(t => new CatalogoItemDto { Id = t.Id, Nombre = t.Nombre })
            .ToListAsync();

        Areas = await _db.Areas
            .Where(a => a.Activo)
            .OrderBy(a => a.Nombre)
            .Select(a => new CatalogoItemDto { Id = a.Id, Nombre = a.Nombre })
            .ToListAsync();

        Motivos = await _db.MotivosVisita
            .Where(m => m.Activo)
            .OrderBy(m => m.Nombre)
            .Select(m => new CatalogoItemDto { Id = m.Id, Nombre = m.Nombre })
            .ToListAsync();
    }
}
