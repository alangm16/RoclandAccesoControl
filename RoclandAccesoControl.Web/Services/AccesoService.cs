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
            .FirstOrDefaultAsync(p => p.Activo && p.NumeroIdentificacion == numId);

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
            null,
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

        return new VisitanteResponse(
            registro.Id, persona.Id, persona.Nombre,
            area!.Nombre, motivo!.Nombre, registro.EstadoAcceso, registro.FechaEntrada,
            persona.TotalVisitas > 0,    // EsRecurrente
            persona.TotalVisitas);
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

        return new ProveedorResponse(
            registro.Id, persona.Id, persona.Nombre,
            persona.Empresa!, motivo!.Nombre, registro.EstadoAcceso, registro.FechaEntrada,
            persona.TotalVisitas > 0,
            persona.TotalVisitas);
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

    // ── Métodos para Guardias ───────────────────────────────────────

    public async Task<IEnumerable<SolicitudPendienteResponse>> ObtenerSolicitudesPendientesAsync()
    {
        var solicitudes = await _db.SolicitudesPendientes
            .Include(s => s.Persona)
            .ThenInclude(p => p.TipoIdentificacion)
            .Where(s => s.Estado == "Pendiente")
            .ToListAsync();

        var respuestas = new List<SolicitudPendienteResponse>();

        foreach (var s in solicitudes)
        {
            string motivo = string.Empty;
            string area = string.Empty;

            if (s.TipoRegistro == "Visitante")
            {
                var reg = await _db.RegistrosVisitantes
                    .Include(r => r.Motivo)
                    .Include(r => r.Area)
                    .FirstOrDefaultAsync(r => r.Id == s.RegistroId);

                motivo = reg?.Motivo?.Nombre ?? "";
                area = reg?.Area?.Nombre ?? "";
            }
            else if (s.TipoRegistro == "Proveedor")
            {
                var reg = await _db.RegistrosProveedores
                    .Include(r => r.Motivo)
                    .FirstOrDefaultAsync(r => r.Id == s.RegistroId);

                motivo = reg?.Motivo?.Nombre ?? "";
            }

            respuestas.Add(new SolicitudPendienteResponse(
                SolicitudId: s.Id,
                RegistroId: s.RegistroId,
                TipoRegistro: s.TipoRegistro,
                PersonaId: s.PersonaId,
                NombrePersona: s.Persona.Nombre,
                Empresa: s.Persona.Empresa,
                NumeroIdentificacion: s.Persona.NumeroIdentificacion,
                TipoID: s.Persona.TipoIdentificacion?.Nombre ?? "",
                Motivo: motivo,
                Area: area,
                FechaSolicitud: s.FechaSolicitud
            ));
        }

        return respuestas.OrderBy(r => r.FechaSolicitud);
    }

    public async Task<IEnumerable<AccesoActivoResponse>> ObtenerAccesosActivosAsync()
    {
        var respuestas = new List<AccesoActivoResponse>();

        // Visitantes activos
        var visitantes = await _db.RegistrosVisitantes
            .Include(r => r.Persona)
            .Include(r => r.Area)
            .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
            .ToListAsync();

        respuestas.AddRange(visitantes.Select(v => new AccesoActivoResponse(
            RegistroId: v.Id,
            TipoRegistro: "Visitante",
            NombrePersona: v.Persona.Nombre,
            Empresa: v.Persona.Empresa,
            NumeroGafete: v.NumeroGafete ?? "",
            FechaEntrada: v.FechaEntrada,
            Area: v.Area.Nombre
        )));

        // Proveedores activos
        var proveedores = await _db.RegistrosProveedores
            .Include(r => r.Persona)
            .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
            .ToListAsync();

        respuestas.AddRange(proveedores.Select(p => new AccesoActivoResponse(
            RegistroId: p.Id,
            TipoRegistro: "Proveedor",
            NombrePersona: p.Persona.Nombre,
            Empresa: p.Persona.Empresa,
            NumeroGafete: p.NumeroGafete ?? "",
            FechaEntrada: p.FechaEntrada,
            Area: "N/A"
        )));

        return respuestas.OrderByDescending(r => r.FechaEntrada);
    }

    public async Task<bool> AprobarSolicitudAsync(AprobarSolicitudRequest request)
    {
        var solicitud = await _db.SolicitudesPendientes.FindAsync(request.SolicitudId);
        if (solicitud == null || solicitud.Estado != "Pendiente") return false;

        solicitud.Estado = "Aprobado";
        solicitud.GuardiaId = request.GuardiaId;

        if (solicitud.TipoRegistro == "Visitante")
        {
            var registro = await _db.RegistrosVisitantes.FindAsync(solicitud.RegistroId);
            if (registro != null)
            {
                registro.EstadoAcceso = "Aprobado";
                registro.GuardiaEntradaId = request.GuardiaId;
                registro.NumeroGafete = request.NumeroGafete;
            }
        }
        else if (solicitud.TipoRegistro == "Proveedor")
        {
            var registro = await _db.RegistrosProveedores.FindAsync(solicitud.RegistroId);
            if (registro != null)
            {
                registro.EstadoAcceso = "Aprobado";
                registro.GuardiaEntradaId = request.GuardiaId;
                registro.NumeroGafete = request.NumeroGafete;
            }
        }

        await _db.SaveChangesAsync();

        // Notificar por SignalR a la web que se aprobó
        await _hub.Clients.All.SendAsync("SolicitudAprobada", request.SolicitudId);

        return true;
    }

    public async Task<bool> RechazarSolicitudAsync(RechazarSolicitudRequest request)
    {
        var solicitud = await _db.SolicitudesPendientes.FindAsync(request.SolicitudId);
        if (solicitud == null || solicitud.Estado != "Pendiente") return false;

        solicitud.Estado = "Rechazado";
        solicitud.GuardiaId = request.GuardiaId;

        if (solicitud.TipoRegistro == "Visitante")
        {
            var registro = await _db.RegistrosVisitantes.FindAsync(solicitud.RegistroId);
            if (registro != null)
            {
                registro.EstadoAcceso = "Rechazado";
                registro.GuardiaEntradaId = request.GuardiaId;
                registro.Observaciones = string.IsNullOrEmpty(registro.Observaciones)
                    ? request.Motivo
                    : $"{registro.Observaciones} | Rechazo: {request.Motivo}";
            }
        }
        else if (solicitud.TipoRegistro == "Proveedor")
        {
            var registro = await _db.RegistrosProveedores.FindAsync(solicitud.RegistroId);
            if (registro != null)
            {
                registro.EstadoAcceso = "Rechazado";
                registro.GuardiaEntradaId = request.GuardiaId;
                registro.Observaciones = string.IsNullOrEmpty(registro.Observaciones)
                    ? request.Motivo
                    : $"{registro.Observaciones} | Rechazo: {request.Motivo}";
            }
        }

        await _db.SaveChangesAsync();

        // Notificar por SignalR a la web que se rechazó
        await _hub.Clients.All.SendAsync("SolicitudRechazada", request.SolicitudId);

        return true;
    }

    public async Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request)
    {
        if (request.TipoRegistro == "Visitante")
        {
            var registro = await _db.RegistrosVisitantes.FindAsync(request.RegistroId);
            if (registro == null || registro.FechaSalida != null) return false;

            registro.FechaSalida = DateTime.Now;
            registro.GuardiaSalidaId = request.GuardiaId;
        }
        else if (request.TipoRegistro == "Proveedor")
        {
            var registro = await _db.RegistrosProveedores.FindAsync(request.RegistroId);
            if (registro == null || registro.FechaSalida != null) return false;

            registro.FechaSalida = DateTime.Now;
            registro.GuardiaSalidaId = request.GuardiaId;
        }
        else
        {
            return false;
        }

        await _db.SaveChangesAsync();
        return true;
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
