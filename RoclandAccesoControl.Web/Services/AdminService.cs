using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RoclandAccesoControl.Web.Data;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Models.Entities;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Services;

public class AdminService : IAdminService
{
    private readonly RoclandDbContext _db;

    public AdminService(RoclandDbContext db) => _db = db;

    // ── KPIs ───────────────────────────────────────────────────────────
    public async Task<DashboardKpiDto> ObtenerKpisAsync()
    {
        var hoy = DateTime.UtcNow.Date;

        var dentroAhora = await _db.RegistrosVisitantes
            .CountAsync(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
            + await _db.RegistrosProveedores
            .CountAsync(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null);

        var visitantesHoy = await _db.RegistrosVisitantes
            .CountAsync(r => r.FechaEntrada.Date == hoy);

        var proveedoresHoy = await _db.RegistrosProveedores
            .CountAsync(r => r.FechaEntrada.Date == hoy);

        var pendientes = await _db.SolicitudesPendientes
            .CountAsync(s => s.Estado == "Pendiente");

        // Promedio de estancia de los que ya salieron hoy
        var minutosVis = await _db.RegistrosVisitantes
            .Where(r => r.FechaEntrada.Date == hoy && r.MinutosEstancia != null)
            .Select(r => (double)r.MinutosEstancia!.Value)
            .ToListAsync();

        var minutosProv = await _db.RegistrosProveedores
            .Where(r => r.FechaEntrada.Date == hoy && r.MinutosEstancia != null)
            .Select(r => (double)r.MinutosEstancia!.Value)
            .ToListAsync();

        var todos = minutosVis.Concat(minutosProv).ToList();
        var promedio = todos.Count > 0 ? todos.Average() : 0;

        return new DashboardKpiDto(
            dentroAhora,
            visitantesHoy + proveedoresHoy,
            visitantesHoy,
            proveedoresHoy,
            Math.Round(promedio, 1),
            pendientes);
    }

    public async Task<IEnumerable<FlujoPorHoraDto>> ObtenerFlujoPorHoraHoyAsync()
    {
        var hoy = DateTime.UtcNow.Date;

        var vis = await _db.RegistrosVisitantes
            .Where(r => r.FechaEntrada.Date == hoy)
            .GroupBy(r => r.FechaEntrada.Hour)
            .Select(g => new { Hora = g.Key, Total = g.Count() })
            .ToListAsync();

        var prov = await _db.RegistrosProveedores
            .Where(r => r.FechaEntrada.Date == hoy)
            .GroupBy(r => r.FechaEntrada.Hour)
            .Select(g => new { Hora = g.Key, Total = g.Count() })
            .ToListAsync();

        return Enumerable.Range(6, 16) // 06:00 - 21:00
            .Select(h => new FlujoPorHoraDto(h,
                (vis.FirstOrDefault(v => v.Hora == h)?.Total ?? 0) +
                (prov.FirstOrDefault(p => p.Hora == h)?.Total ?? 0)));
    }

    public async Task<IEnumerable<FlujoDiarioDto>> ObtenerFlujoDiarioMesAsync(int anio, int mes)
    {
        var vis = await _db.RegistrosVisitantes
            .Where(r => r.FechaEntrada.Year == anio && r.FechaEntrada.Month == mes)
            .GroupBy(r => r.FechaEntrada.Date)
            .Select(g => new { Fecha = g.Key, Total = g.Count() })
            .ToListAsync();

        var prov = await _db.RegistrosProveedores
            .Where(r => r.FechaEntrada.Year == anio && r.FechaEntrada.Month == mes)
            .GroupBy(r => r.FechaEntrada.Date)
            .Select(g => new { Fecha = g.Key, Total = g.Count() })
            .ToListAsync();

        var diasEnMes = DateTime.DaysInMonth(anio, mes);
        return Enumerable.Range(1, diasEnMes).Select(d =>
        {
            var fecha = new DateTime(anio, mes, d);
            return new FlujoDiarioDto(
                fecha.ToString("dd/MM"),
                vis.FirstOrDefault(v => v.Fecha == fecha)?.Total ?? 0,
                prov.FirstOrDefault(p => p.Fecha == fecha)?.Total ?? 0);
        });
    }

    public async Task<IEnumerable<AreaVisitadaDto>> ObtenerAreasMasVisitadasAsync(int dias = 30)
    {
        //var desde = DateTime.UtcNow.AddDays(-dias);
        //return await _db.RegistrosVisitantes
        //    .Where(r => r.FechaEntrada >= desde)
        //    .GroupBy(r => r.Area.Nombre)
        //    .Select(g => new AreaVisitadaDto(g.Key, g.Count()))
        //    .OrderByDescending(a => a.Total)
        //    .Take(8)
        //    .ToListAsync();
        return null;
    }

    // ── Historial ──────────────────────────────────────────────────────
    public async Task<(IEnumerable<HistorialAccesoDto> Items, int Total)> ObtenerHistorialAsync(
        string? busqueda, string? tipo, DateTime? desde, DateTime? hasta,
        int pagina, int porPagina)
    {
        //hasta = hasta?.AddDays(1); // incluir todo el día final

        //var qVis = _db.RegistrosVisitantes
        //    .Include(r => r.Persona).ThenInclude(p => p.TipoIdentificacion)
        //    .Include(r => r.Area).Include(r => r.Motivo).Include(r => r.GuardiaEntrada)
        //    .Where(r => tipo == null || tipo == "Visitante")
        //    .Where(r => desde == null || r.FechaEntrada >= desde)
        //    .Where(r => hasta == null || r.FechaEntrada <= hasta)
        //    .Where(r => busqueda == null ||
        //        r.Persona.Nombre.Contains(busqueda) ||
        //        r.Persona.NumeroIdentificacion.Contains(busqueda))
        //    .Select(r => new HistorialAccesoDto(
        //        r.Id, "Visitante", r.Persona.Nombre, null,
        //        r.Persona.NumeroIdentificacion, r.Area.Nombre,
        //        r.Motivo.Nombre, r.FechaEntrada, r.FechaSalida,
        //        r.MinutosEstancia, r.EstadoAcceso, r.NumeroGafete,
        //        r.GuardiaEntrada.Nombre));

        //var qProv = _db.RegistrosProveedores
        //    .Include(r => r.Persona).ThenInclude(p => p.TipoIdentificacion)
        //    .Include(r => r.Motivo).Include(r => r.GuardiaEntrada)
        //    .Where(r => tipo == null || tipo == "Proveedor")
        //    .Where(r => desde == null || r.FechaEntrada >= desde)
        //    .Where(r => hasta == null || r.FechaEntrada <= hasta)
        //    .Where(r => busqueda == null ||
        //        r.Persona.Nombre.Contains(busqueda) ||
        //        r.Persona.NumeroIdentificacion.Contains(busqueda) ||
        //        (r.Persona.Empresa != null && r.Persona.Empresa.Contains(busqueda)))
        //    .Select(r => new HistorialAccesoDto(
        //        r.Id, "Proveedor", r.Persona.Nombre, r.Persona.Empresa,
        //        r.Persona.NumeroIdentificacion, null,
        //        r.Motivo.Nombre, r.FechaEntrada, r.FechaSalida,
        //        r.MinutosEstancia, r.EstadoAcceso, r.NumeroGafete,
        //        r.GuardiaEntrada.Nombre));

        //var union = qVis.Union(qProv).OrderByDescending(r => r.FechaEntrada);
        //var total = await union.CountAsync();
        //var items = await union.Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

        //return (items, total);
        return (null, 2);
    }

    // ── Personas ───────────────────────────────────────────────────────
    public async Task<IEnumerable<PersonaPerfilDto>> ObtenerPersonasFrecuentesAsync(int top = 20)
    {
        return await _db.Personas
            .Include(p => p.TipoIdentificacion)
            .Where(p => p.Activo && p.TotalVisitas > 0)
            .OrderByDescending(p => p.TotalVisitas)
            .Take(top)
            .Select(p => new PersonaPerfilDto(
                p.Id, p.Nombre, p.TipoIdentificacion.Nombre,
                p.NumeroIdentificacion, p.Empresa,
                p.Telefono, p.Email, p.TotalVisitas,
                p.FechaRegistro, p.FechaUltimaVisita))
            .ToListAsync();
    }

    public async Task<PersonaPerfilDto?> ObtenerPerfilPersonaAsync(int id)
    {
        var p = await _db.Personas
            .Include(p => p.TipoIdentificacion)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (p is null) return null;

        return new PersonaPerfilDto(p.Id, p.Nombre, p.TipoIdentificacion.Nombre,
            p.NumeroIdentificacion, p.Empresa, p.Telefono, p.Email,
            p.TotalVisitas, p.FechaRegistro, p.FechaUltimaVisita);
    }

    public async Task<IEnumerable<HistorialAccesoDto>> ObtenerHistorialPersonaAsync(int personaId)
    {
        var vis = await _db.RegistrosVisitantes
            .Include(r => r.Area).Include(r => r.Motivo).Include(r => r.GuardiaEntrada)
            .Where(r => r.PersonaId == personaId)
            .Select(r => new HistorialAccesoDto(
                r.Id, "Visitante", r.Persona.Nombre, null,
                r.Persona.NumeroIdentificacion, r.Area.Nombre,
                r.Motivo.Nombre, r.FechaEntrada, r.FechaSalida,
                r.MinutosEstancia, r.EstadoAcceso, r.NumeroGafete,
                r.GuardiaEntrada.Nombre))
            .ToListAsync();

        var prov = await _db.RegistrosProveedores
            .Include(r => r.Motivo).Include(r => r.GuardiaEntrada)
            .Where(r => r.PersonaId == personaId)
            .Select(r => new HistorialAccesoDto(
                r.Id, "Proveedor", r.Persona.Nombre, r.Persona.Empresa,
                r.Persona.NumeroIdentificacion, null,
                r.Motivo.Nombre, r.FechaEntrada, r.FechaSalida,
                r.MinutosEstancia, r.EstadoAcceso, r.NumeroGafete,
                r.GuardiaEntrada.Nombre))
            .ToListAsync();

        return vis.Concat(prov).OrderByDescending(r => r.FechaEntrada);
    }

    // ── Catálogos ──────────────────────────────────────────────────────
    public async Task<bool> CrearAreaAsync(CatalogoCreateDto dto)
    {
        _db.Areas.Add(new Area { Nombre = dto.Nombre });
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> ToggleAreaAsync(int id)
    {
        var a = await _db.Areas.FindAsync(id);
        if (a is null) return false;
        a.Activo = !a.Activo;
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> CrearMotivoAsync(CatalogoCreateDto dto)
    {
        _db.MotivosVisita.Add(new MotivoVisita { Nombre = dto.Nombre });
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> ToggleMotivoAsync(int id)
    {
        var m = await _db.MotivosVisita.FindAsync(id);
        if (m is null) return false;
        m.Activo = !m.Activo;
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> CrearTipoIdAsync(CatalogoCreateDto dto)
    {
        _db.TiposIdentificacion.Add(new TipoIdentificacion { Nombre = dto.Nombre });
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> ToggleTipoIdAsync(int id)
    {
        var t = await _db.TiposIdentificacion.FindAsync(id);
        if (t is null) return false;
        t.Activo = !t.Activo;
        return await _db.SaveChangesAsync() > 0;
    }

    // ── Guardias ───────────────────────────────────────────────────────
    public async Task<IEnumerable<Guardia>> ObtenerGuardiasAsync()
        => await _db.Guardias.OrderBy(g => g.Nombre).ToListAsync();

    public async Task<bool> CrearGuardiaAsync(GuardiaCreateDto dto)
    {
        if (await _db.Guardias.AnyAsync(g => g.Usuario == dto.Usuario))
            return false;

        _db.Guardias.Add(new Guardia
        {
            Nombre = dto.Nombre,
            Usuario = dto.Usuario,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        });
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> ActualizarGuardiaAsync(int id, GuardiaUpdateDto dto)
    {
        var g = await _db.Guardias.FindAsync(id);
        if (g is null) return false;
        g.Nombre = dto.Nombre;
        g.Activo = dto.Activo;
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> ResetPasswordGuardiaAsync(int id, string nuevaPassword)
    {
        var g = await _db.Guardias.FindAsync(id);
        if (g is null) return false;
        g.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);
        return await _db.SaveChangesAsync() > 0;
    }

    // ── Exportar Excel ─────────────────────────────────────────────────
    public async Task<byte[]> ExportarExcelHoyAsync()
    {
        var hoy = DateTime.UtcNow.Date;
        var (items, _) = await ObtenerHistorialAsync(null, null, hoy, hoy, 1, 9999);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Accesos del día");

        // Encabezados
        var headers = new[] { "Tipo", "Nombre", "Empresa", "No. ID",
            "Área / Empresa", "Motivo", "Entrada", "Salida",
            "Minutos", "Estado", "Gafete", "Guardia" };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Datos
        int row = 2;
        foreach (var item in items)
        {
            ws.Cell(row, 1).Value = item.Tipo;
            ws.Cell(row, 2).Value = item.Nombre;
            ws.Cell(row, 3).Value = item.Empresa ?? "";
            ws.Cell(row, 4).Value = item.NumeroIdentificacion;
            ws.Cell(row, 5).Value = item.Area ?? "";
            ws.Cell(row, 6).Value = item.Motivo;
            ws.Cell(row, 7).Value = item.FechaEntrada.ToLocalTime().ToString("HH:mm");
            ws.Cell(row, 8).Value = item.FechaSalida?.ToLocalTime().ToString("HH:mm") ?? "—";
            ws.Cell(row, 9).Value = item.MinutosEstancia?.ToString() ?? "—";
            ws.Cell(row, 10).Value = item.EstadoAcceso;
            ws.Cell(row, 11).Value = item.NumeroGafete ?? "—";
            ws.Cell(row, 12).Value = item.Guardia;

            // Color por estado
            var color = item.EstadoAcceso switch
            {
                "Aprobado" or "Salido" => XLColor.FromHtml("#F0FDF4"),
                "Rechazado" => XLColor.FromHtml("#FEF2F2"),
                _ => XLColor.FromHtml("#FFFBEB")
            };
            ws.Row(row).Style.Fill.BackgroundColor = color;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Exportar PDF ───────────────────────────────────────────────────
    public async Task<byte[]> ExportarPdfHoyAsync()
    {
        //QuestPDF.Settings.License = LicenseType.Community;

        //var hoy = DateTime.UtcNow.Date;
        //var (items, total) = await ObtenerHistorialAsync(null, null, hoy, hoy, 1, 9999);
        //var lista = items.ToList();

        //var doc = Document.Create(container =>
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4.Landscape());
        //        page.Margin(1.5f, Unit.Centimetre);
        //        page.DefaultTextStyle(t => t.FontSize(9));

        //        page.Header().Column(col =>
        //        {
        //            col.Item().Row(row =>
        //            {
        //                row.RelativeItem().Column(c =>
        //                {
        //                    c.Item().Text("ROCLAND — Control de Acceso")
        //                        .FontSize(16).Bold().FontColor("#1E293B");
        //                    c.Item().Text($"Reporte del día: {hoy:dd/MM/yyyy}  |  Total: {total} accesos")
        //                        .FontSize(10).FontColor("#64748B");
        //                });
        //                row.ConstantItem(80).AlignRight()
        //                    .Text(DateTime.Now.ToString("HH:mm"))
        //                    .FontSize(10).FontColor("#94A3B8");
        //            });
        //            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#E2E8F0");
        //        });

        //        page.Content().PaddingTop(12).Table(table =>
        //        {
        //            table.ColumnsDefinition(cols =>
        //            {
        //                cols.ConstantColumn(60);  // Tipo
        //                cols.RelativeColumn(2);   // Nombre
        //                cols.RelativeColumn(1.5f);// Empresa
        //                cols.RelativeColumn(1.5f);// ID
        //                cols.RelativeColumn(1.5f);// Área
        //                cols.RelativeColumn(1.5f);// Motivo
        //                cols.ConstantColumn(45);  // Entrada
        //                cols.ConstantColumn(45);  // Salida
        //                cols.ConstantColumn(40);  // Min
        //                cols.ConstantColumn(60);  // Estado
        //                cols.ConstantColumn(40);  // Gafete
        //            });

        //            // Header
        //            foreach (var h in new[] { "Tipo", "Nombre", "Empresa", "No. ID",
        //                "Área", "Motivo", "Entrada", "Salida", "Min", "Estado", "Gafete" })
        //            {
        //                table.Header().Cell().Background("#1E293B").Padding(4)
        //                    .Text(h).FontColor(Colors.White).Bold().FontSize(8);
        //            }

        //            // Filas
        //            bool alt = false;
        //            foreach (var item in lista)
        //            {
        //                var bg = alt ? "#F8FAFC" : Colors.White;
        //                alt = !alt;

        //                var estadoColor = item.EstadoAcceso switch
        //                {
        //                    "Aprobado" or "Salido" => "#16A34A",
        //                    "Rechazado" => "#DC2626",
        //                    _ => "#D97706"
        //                };

        //                void Cell(string text, string? color = null) =>
        //                    table.Cell().Background(bg).Padding(4)
        //                        .Text(text).FontColor(color ?? "#1E293B").FontSize(8);

        //                Cell(item.Tipo, item.Tipo == "Visitante" ? "#2563EB" : "#7C3AED");
        //                Cell(item.Nombre);
        //                Cell(item.Empresa ?? "—");
        //                Cell(item.NumeroIdentificacion);
        //                Cell(item.Area ?? "—");
        //                Cell(item.Motivo);
        //                Cell(item.FechaEntrada.ToLocalTime().ToString("HH:mm"));
        //                Cell(item.FechaSalida?.ToLocalTime().ToString("HH:mm") ?? "—");
        //                Cell(item.MinutosEstancia?.ToString() ?? "—");
        //                Cell(item.EstadoAcceso, estadoColor);
        //                Cell(item.NumeroGafete ?? "—");
        //            }
        //        });

        //        page.Footer().AlignCenter()
        //            .Text(t =>
        //            {
        //                t.Span("Rocland AccesoControl  ·  Página ").FontColor("#94A3B8");
        //                t.CurrentPageNumber().FontColor("#94A3B8");
        //                t.Span(" de ").FontColor("#94A3B8");
        //                t.TotalPages().FontColor("#94A3B8");
        //            });
        //    });
        //});

        return null;/*doc.GeneratePdf();*/
    }
}