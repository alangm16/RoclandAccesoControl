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
    private readonly ILogger<AccesoService> _logger;

    public AccesoService(
        RoclandDbContext db,
        IHubContext<AccesoHub> hub,
        ILogger<AccesoService> logger)
    {
        _db     = db;
        _hub    = hub;
        _logger = logger;
    }

    // ── Buscar persona por número de identificación ─────────────────
    public async Task<PersonaBusquedaResponse?> BuscarPersonaAsync(string numId)
    {
        var persona = await _db.Personas
            .Include(p => p.TipoIdentificacion)
            .Where(p => p.Activo && p.NumeroIdentificacion.Contains(numId))
            .OrderByDescending(p => p.TotalVisitas)
            .FirstOrDefaultAsync();

        if (persona is null) return null;

        return new PersonaBusquedaResponse
        {
            Id                   = persona.Id,
            Nombre               = persona.Nombre,
            TipoId               = persona.TipoIdentificacion?.Nombre ?? string.Empty,
            TipoIdentificacionId = persona.TipoIdentificacionId,
            NumeroIdentificacion = persona.NumeroIdentificacion,
            Empresa              = persona.Empresa,
            Telefono             = persona.Telefono,
            Email                = persona.Email,
            TotalVisitas         = persona.TotalVisitas,
            FechaUltimaVisita    = persona.FechaUltimaVisita,
        };
    }

    // ── Registrar visitante ─────────────────────────────────────────
    public async Task<VisitanteResponse> RegistrarVisitanteAsync(
        CrearVisitanteRequest req, string ip)
    {
        // 1. Obtener o crear perfil de persona
        var persona = await ObtenerOCrearPersonaAsync(
            req.TipoIdentificacionId,
            req.NumeroIdentificacion,
            req.Nombre,
            req.Empresa,
            req.Telefono,
            req.Email);

        // 2. Crear registro de visitante
        // GuardiaEntradaId = 1 (guardia sistema/pendiente).
        // La aprobación real la hace el guardia desde la app móvil.
        var registro = new RegistroVisitante
        {
            PersonaId             = persona.Id,
            AreaId                = req.AreaId,
            MotivoId              = req.MotivoId,
            FechaEntrada          = DateTime.Now,
            GuardiaEntradaId      = 1,               // pendiente de asignación por guardia
            EstadoAcceso          = "Pendiente",
            ConsentimientoFirmado = req.ConsentimientoFirmado,
            Observaciones         = req.Observaciones,
            IPSolicitud           = ip,
            FechaCreacion         = DateTime.Now,
        };

        _db.RegistrosVisitantes.Add(registro);

        // 3. Crear solicitud pendiente para SignalR
        var solicitud = new SolicitudPendiente
        {
            TipoRegistro  = "Visitante",
            RegistroId    = 0,     // se actualiza tras SaveChanges
            PersonaId     = persona.Id,
            FechaSolicitud = DateTime.Now,
            Estado        = "Pendiente",
        };
        _db.SolicitudesPendientes.Add(solicitud);

        await _db.SaveChangesAsync();

        // Actualizar RegistroId ahora que tenemos el Id
        solicitud.RegistroId = registro.Id;
        await _db.SaveChangesAsync();

        // 4. Actualizar contadores de visita
        await ActualizarContadorAsync(persona.Id);

        // 5. Notificar a guardias vía SignalR
        await _hub.Clients.All.SendAsync("NuevaSolicitud", new
        {
            solicitudId  = solicitud.Id,
            tipo         = "Visitante",
            registroId   = registro.Id,
            personaId    = persona.Id,
            nombre       = persona.Nombre,
            empresa      = persona.Empresa,
            numId        = persona.NumeroIdentificacion,
            motivo       = req.MotivoId,
            hora         = registro.FechaEntrada.ToString("HH:mm"),
        });

        _logger.LogInformation(
            "Visitante registrado: PersonaId={PersonaId}, RegistroId={RegistroId}",
            persona.Id, registro.Id);

        // 6. Obtener nombres para la respuesta
        var area   = await _db.Areas.FindAsync(req.AreaId);
        var motivo = await _db.MotivosVisita.FindAsync(req.MotivoId);

        return new VisitanteResponse
        {
            Id           = registro.Id,
            PersonaId    = persona.Id,
            Nombre       = persona.Nombre,
            Area         = area?.Nombre   ?? string.Empty,
            Motivo       = motivo?.Nombre ?? string.Empty,
            EstadoAcceso = registro.EstadoAcceso,
            FechaEntrada = registro.FechaEntrada,
            Mensaje      = "Solicitud recibida. Espera autorización del guardia.",
        };
    }

    // ── Registrar proveedor ─────────────────────────────────────────
    public async Task<ProveedorResponse> RegistrarProveedorAsync(
        CrearProveedorRequest req, string ip)
    {
        var persona = await ObtenerOCrearPersonaAsync(
            req.TipoIdentificacionId,
            req.NumeroIdentificacion,
            req.Nombre,
            req.Empresa,
            req.Telefono,
            req.Email);

        var registro = new RegistroProveedor
        {
            PersonaId             = persona.Id,
            MotivoId              = req.MotivoId,
            FechaEntrada          = DateTime.Now,
            UnidadPlacas          = req.UnidadPlacas,
            FacturaRemision       = req.FacturaRemision,
            GuardiaEntradaId      = 1,
            EstadoAcceso          = "Pendiente",
            ConsentimientoFirmado = req.ConsentimientoFirmado,
            Observaciones         = req.Observaciones,
            IPSolicitud           = ip,
            FechaCreacion         = DateTime.Now,
        };

        _db.RegistrosProveedores.Add(registro);

        var solicitud = new SolicitudPendiente
        {
            TipoRegistro   = "Proveedor",
            RegistroId     = 0,
            PersonaId      = persona.Id,
            FechaSolicitud = DateTime.Now,
            Estado         = "Pendiente",
        };
        _db.SolicitudesPendientes.Add(solicitud);

        await _db.SaveChangesAsync();

        solicitud.RegistroId = registro.Id;
        await _db.SaveChangesAsync();

        await ActualizarContadorAsync(persona.Id);

        await _hub.Clients.All.SendAsync("NuevaSolicitud", new
        {
            solicitudId = solicitud.Id,
            tipo        = "Proveedor",
            registroId  = registro.Id,
            personaId   = persona.Id,
            nombre      = persona.Nombre,
            empresa     = persona.Empresa,
            numId       = persona.NumeroIdentificacion,
            placas      = registro.UnidadPlacas,
            factura     = registro.FacturaRemision,
            motivo      = req.MotivoId,
            hora        = registro.FechaEntrada.ToString("HH:mm"),
        });

        _logger.LogInformation(
            "Proveedor registrado: PersonaId={PersonaId}, RegistroId={RegistroId}",
            persona.Id, registro.Id);

        var motivo = await _db.MotivosVisita.FindAsync(req.MotivoId);

        return new ProveedorResponse
        {
            Id           = registro.Id,
            PersonaId    = persona.Id,
            Nombre       = persona.Nombre,
            Empresa      = persona.Empresa ?? string.Empty,
            Motivo       = motivo?.Nombre  ?? string.Empty,
            EstadoAcceso = registro.EstadoAcceso,
            FechaEntrada = registro.FechaEntrada,
            Mensaje      = "Solicitud recibida. Espera autorización del guardia.",
        };
    }

    // ── Helpers privados ────────────────────────────────────────────

    /// <summary>
    /// Busca un perfil por TipoId+NumeroId; si no existe, lo crea.
    /// Si ya existe pero cambió el nombre, actualiza el nombre.
    /// </summary>
    private async Task<Persona> ObtenerOCrearPersonaAsync(
        int tipoId, string numId, string nombre,
        string? empresa, string? telefono, string? email)
    {
        var persona = await _db.Personas
            .FirstOrDefaultAsync(p =>
                p.TipoIdentificacionId == tipoId &&
                p.NumeroIdentificacion  == numId);

        if (persona is null)
        {
            persona = new Persona
            {
                TipoIdentificacionId = tipoId,
                NumeroIdentificacion = numId,
                Nombre               = nombre,
                Empresa              = empresa,
                Telefono             = telefono,
                Email                = email,
                FechaRegistro        = DateTime.Now,
                TotalVisitas         = 0,
                Activo               = true,
            };
            _db.Personas.Add(persona);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Actualizar datos opcionales si hay cambio
            bool changed = false;
            if (persona.Nombre != nombre) { persona.Nombre = nombre; changed = true; }
            if (empresa  is not null && persona.Empresa  != empresa)  { persona.Empresa  = empresa;  changed = true; }
            if (telefono is not null && persona.Telefono != telefono) { persona.Telefono = telefono; changed = true; }
            if (email    is not null && persona.Email    != email)    { persona.Email    = email;    changed = true; }
            if (changed) await _db.SaveChangesAsync();
        }

        return persona;
    }

    private async Task ActualizarContadorAsync(int personaId)
    {
        var persona = await _db.Personas.FindAsync(personaId);
        if (persona is null) return;
        persona.TotalVisitas++;
        persona.FechaUltimaVisita = DateTime.Now;
        await _db.SaveChangesAsync();
    }
}
