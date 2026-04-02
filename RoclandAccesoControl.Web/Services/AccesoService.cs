using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RoclandAccesoControl.Web.Data;
using RoclandAccesoControl.Web.Hubs;
using RoclandAccesoControl.Web.Models.DTOs;
using RoclandAccesoControl.Web.Models.Entities;
using RoclandAccesoControl.Web.Services.Interfaces;

namespace RoclandAccesoControl.Web.Services;

public class AccesoService : IAccesoService
{
    private readonly RoclandDbContext _db;
    private readonly IHubContext<AccesoHub> _hub;
    private readonly IConfiguration _config;

    public AccesoService(RoclandDbContext db, IHubContext<AccesoHub> hub, IConfiguration config)
    {
        _db = db;
        _hub = hub;
        _config = config;
    }

    // ── Buscar persona por número de identificación ────────────────────
    public async Task<PersonaBusquedaResponse?> BuscarPersonaAsync(string numeroId)
    {
        var persona = await _db.Personas
            .Include(p => p.TipoIdentificacion)
            .Where(p => p.NumeroIdentificacion.Contains(numeroId) && p.Activo)
            .FirstOrDefaultAsync();

        if (persona is null) return null;

        return new PersonaBusquedaResponse(
            persona.Id,
            persona.Nombre,
            persona.TipoIdentificacion.Nombre,
            persona.NumeroIdentificacion,
            persona.Empresa,
            persona.Telefono,
            persona.Email,
            persona.TotalVisitas,
            persona.FechaUltimaVisita
        );
    }

    // ── Registrar visitante ────────────────────────────────────────────
    public async Task<VisitanteResponse> RegistrarVisitanteAsync(
        CrearVisitanteRequest request, string ipSolicitud)
    {
        // Obtener o crear la persona
        var persona = await ObtenerOCrearPersonaAsync(
            request.TipoIdentificacionId,
            request.NumeroIdentificacion,
            request.Nombre,
            null, request.Telefono, request.Email);

        // Guardia de sistema para solicitudes desde formulario web
        // (se asigna el Id del primer guardia activo como placeholder;
        //  el guardia real queda registrado al aprobar)
        var guardiaDefault = await _db.Guardias.FirstAsync(g => g.Activo);

        var registro = new RegistroVisitante
        {
            PersonaId = persona.Id,
            AreaId = request.AreaId,
            MotivoId = request.MotivoId,
            FechaEntrada = DateTime.UtcNow,
            GuardiaEntradaId = guardiaDefault.Id,
            EstadoAcceso = "Pendiente",
            ConsentimientoFirmado = request.ConsentimientoFirmado,
            Observaciones = request.Observaciones,
            IPSolicitud = ipSolicitud
        };

        _db.RegistrosVisitantes.Add(registro);
        await _db.SaveChangesAsync();

        // Crear solicitud pendiente para SignalR
        var solicitud = await CrearSolicitudPendienteAsync("Visitante", registro.Id, persona.Id);

        // Notificar a guardias vía SignalR
        var area = await _db.Areas.FindAsync(request.AreaId);
        var motivo = await _db.MotivosVisita.FindAsync(request.MotivoId);

        await _hub.Clients.Group("Guardias").SendAsync("NuevaSolicitud",
            new NuevaSolicitudEvent(
                solicitud.Id, registro.Id, "Visitante",
                persona.Nombre, null, persona.NumeroIdentificacion,
                persona.TipoIdentificacion!.Nombre,
                motivo!.Nombre, area!.Nombre,
                solicitud.FechaSolicitud));

        return new VisitanteResponse(
            registro.Id, persona.Id, persona.Nombre,
            area!.Nombre, motivo!.Nombre, registro.EstadoAcceso, registro.FechaEntrada);
    }

    // ── Registrar proveedor ────────────────────────────────────────────
    public async Task<ProveedorResponse> RegistrarProveedorAsync(
        CrearProveedorRequest request, string ipSolicitud)
    {
        var persona = await ObtenerOCrearPersonaAsync(
            request.TipoIdentificacionId,
            request.NumeroIdentificacion,
            request.Nombre,
            request.Empresa, request.Telefono, request.Email);

        var guardiaDefault = await _db.Guardias.FirstAsync(g => g.Activo);

        var registro = new RegistroProveedor
        {
            PersonaId = persona.Id,
            MotivoId = request.MotivoId,
            FechaEntrada = DateTime.UtcNow,
            UnidadPlacas = request.UnidadPlacas,
            FacturaRemision = request.FacturaRemision,
            GuardiaEntradaId = guardiaDefault.Id,
            EstadoAcceso = "Pendiente",
            ConsentimientoFirmado = request.ConsentimientoFirmado,
            Observaciones = request.Observaciones,
            IPSolicitud = ipSolicitud
        };

        _db.RegistrosProveedores.Add(registro);
        await _db.SaveChangesAsync();

        var solicitud = await CrearSolicitudPendienteAsync("Proveedor", registro.Id, persona.Id);
        var motivo = await _db.MotivosVisita.FindAsync(request.MotivoId);

        await _hub.Clients.Group("Guardias").SendAsync("NuevaSolicitud",
            new NuevaSolicitudEvent(
                solicitud.Id, registro.Id, "Proveedor",
                persona.Nombre, persona.Empresa, persona.NumeroIdentificacion,
                persona.TipoIdentificacion!.Nombre,
                motivo!.Nombre, null,
                solicitud.FechaSolicitud));

        return new ProveedorResponse(
            registro.Id, persona.Id, persona.Nombre,
            persona.Empresa!, motivo!.Nombre, registro.EstadoAcceso, registro.FechaEntrada);
    }

    // ── Aprobar solicitud ──────────────────────────────────────────────
    public async Task<bool> AprobarSolicitudAsync(AprobarSolicitudRequest request)
    {
        var solicitud = await _db.SolicitudesPendientes
            .FindAsync(request.SolicitudId);
        if (solicitud is null || solicitud.Estado != "Pendiente") return false;

        solicitud.Estado = "Aprobado";
        solicitud.GuardiaId = request.GuardiaId;

        // Actualizar el registro correspondiente
        if (solicitud.TipoRegistro == "Visitante")
        {
            var reg = await _db.RegistrosVisitantes.FindAsync(solicitud.RegistroId);
            if (reg is null) return false;
            reg.EstadoAcceso = "Aprobado";
            reg.NumeroGafete = request.NumeroGafete;
            reg.GuardiaEntradaId = request.GuardiaId;
        }
        else
        {
            var reg = await _db.RegistrosProveedores.FindAsync(solicitud.RegistroId);
            if (reg is null) return false;
            reg.EstadoAcceso = "Aprobado";
            reg.NumeroGafete = request.NumeroGafete;
            reg.GuardiaEntradaId = request.GuardiaId;
        }

        // Actualizar contador de visitas de la persona
        var persona = await _db.Personas.FindAsync(solicitud.PersonaId);
        if (persona is not null)
        {
            persona.TotalVisitas++;
            persona.FechaUltimaVisita = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var guardia = await _db.Guardias.FindAsync(request.GuardiaId);
        await _hub.Clients.Group("Guardias").SendAsync("SolicitudResuelta",
            new SolicitudResueltaEvent(request.SolicitudId, "Aprobado", guardia?.Nombre ?? ""));

        return true;
    }

    // ── Rechazar solicitud ─────────────────────────────────────────────
    public async Task<bool> RechazarSolicitudAsync(RechazarSolicitudRequest request)
    {
        var solicitud = await _db.SolicitudesPendientes
            .FindAsync(request.SolicitudId);
        if (solicitud is null || solicitud.Estado != "Pendiente") return false;

        solicitud.Estado = "Rechazado";
        solicitud.GuardiaId = request.GuardiaId;

        if (solicitud.TipoRegistro == "Visitante")
        {
            var reg = await _db.RegistrosVisitantes.FindAsync(solicitud.RegistroId);
            if (reg is not null) reg.EstadoAcceso = "Rechazado";
        }
        else
        {
            var reg = await _db.RegistrosProveedores.FindAsync(solicitud.RegistroId);
            if (reg is not null) reg.EstadoAcceso = "Rechazado";
        }

        await _db.SaveChangesAsync();

        var guardia = await _db.Guardias.FindAsync(request.GuardiaId);
        await _hub.Clients.Group("Guardias").SendAsync("SolicitudResuelta",
            new SolicitudResueltaEvent(request.SolicitudId, "Rechazado", guardia?.Nombre ?? ""));

        return true;
    }

    // ── Marcar salida ──────────────────────────────────────────────────
    public async Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request)
    {
        if (request.TipoRegistro == "Visitante")
        {
            var reg = await _db.RegistrosVisitantes.FindAsync(request.RegistroId);
            if (reg is null || reg.FechaSalida is not null) return false;
            reg.FechaSalida = DateTime.UtcNow;
            reg.EstadoAcceso = "Salido";
            reg.GuardiaSalidaId = request.GuardiaId;
        }
        else
        {
            var reg = await _db.RegistrosProveedores.FindAsync(request.RegistroId);
            if (reg is null || reg.FechaSalida is not null) return false;
            reg.FechaSalida = DateTime.UtcNow;
            reg.EstadoAcceso = "Salido";
            reg.GuardiaSalidaId = request.GuardiaId;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    // ── Obtener solicitudes pendientes ─────────────────────────────────
    public async Task<IEnumerable<SolicitudPendienteResponse>> ObtenerSolicitudesPendientesAsync()
    {
        var solicitudes = await _db.SolicitudesPendientes
            .Include(s => s.Persona)
                .ThenInclude(p => p.TipoIdentificacion)
            .Where(s => s.Estado == "Pendiente")
            .OrderBy(s => s.FechaSolicitud)
            .ToListAsync();

        var result = new List<SolicitudPendienteResponse>();

        foreach (var s in solicitudes)
        {
            string motivo = "", area = "";

            if (s.TipoRegistro == "Visitante")
            {
                var reg = await _db.RegistrosVisitantes
                    .Include(r => r.Motivo)
                    .Include(r => r.Area)
                    .FirstOrDefaultAsync(r => r.Id == s.RegistroId);
                motivo = reg?.Motivo.Nombre ?? "";
                area = reg?.Area.Nombre ?? "";
            }
            else
            {
                var reg = await _db.RegistrosProveedores
                    .Include(r => r.Motivo)
                    .FirstOrDefaultAsync(r => r.Id == s.RegistroId);
                motivo = reg?.Motivo.Nombre ?? "";
            }

            result.Add(new SolicitudPendienteResponse(
                s.Id, s.RegistroId, s.TipoRegistro,
                s.PersonaId, s.Persona.Nombre, s.Persona.Empresa,
                s.Persona.NumeroIdentificacion,
                s.Persona.TipoIdentificacion.Nombre,
                motivo, area, s.FechaSolicitud));
        }

        return result;
    }

    // ── Obtener accesos activos (personas dentro) ──────────────────────
    public async Task<IEnumerable<AccesoActivoResponse>> ObtenerAccesosActivosAsync()
    {
        var visitantes = await _db.RegistrosVisitantes
            .Include(r => r.Persona)
            .Include(r => r.Area)
            .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
            .Select(r => new AccesoActivoResponse(
                r.Id, "Visitante", r.Persona.Nombre, null,
                r.NumeroGafete ?? "", r.FechaEntrada, r.Area.Nombre))
            .ToListAsync();

        var proveedores = await _db.RegistrosProveedores
            .Include(r => r.Persona)
            .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
            .Select(r => new AccesoActivoResponse(
                r.Id, "Proveedor", r.Persona.Nombre, r.Persona.Empresa,
                r.NumeroGafete ?? "", r.FechaEntrada, ""))
            .ToListAsync();

        return visitantes.Concat(proveedores).OrderBy(a => a.FechaEntrada);
    }

    // ── Helpers privados ───────────────────────────────────────────────
    private async Task<Persona> ObtenerOCrearPersonaAsync(
        int tipoIdId, string numeroId, string nombre,
        string? empresa, string? telefono, string? email)
    {
        var persona = await _db.Personas
            .Include(p => p.TipoIdentificacion)
            .FirstOrDefaultAsync(p =>
                p.TipoIdentificacionId == tipoIdId &&
                p.NumeroIdentificacion == numeroId);

        if (persona is null)
        {
            persona = new Persona
            {
                Nombre = nombre,
                TipoIdentificacionId = tipoIdId,
                NumeroIdentificacion = numeroId,
                Empresa = empresa,
                Telefono = telefono,
                Email = email
            };
            _db.Personas.Add(persona);
            await _db.SaveChangesAsync();

            // Recargar con navegación
            persona = await _db.Personas
                .Include(p => p.TipoIdentificacion)
                .FirstAsync(p => p.Id == persona.Id);
        }

        return persona;
    }

    private async Task<SolicitudPendiente> CrearSolicitudPendienteAsync(
        string tipo, int registroId, int personaId)
    {
        var solicitud = new SolicitudPendiente
        {
            TipoRegistro = tipo,
            RegistroId = registroId,
            PersonaId = personaId
        };
        _db.SolicitudesPendientes.Add(solicitud);
        await _db.SaveChangesAsync();
        return solicitud;
    }
}