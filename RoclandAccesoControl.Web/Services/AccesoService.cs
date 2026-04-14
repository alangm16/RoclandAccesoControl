using DocumentFormat.OpenXml.InkML;
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
    private readonly IFcmService _fcm;
    private static readonly TimeSpan _offsetMexico = TimeSpan.FromHours(-6);
    public AccesoService(
        RoclandDbContext db,
        IHubContext<AccesoHub> hub,
        ILogger<AccesoService> logger,
        IFcmService fcm)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
        _fcm = fcm;
    }

    public async Task<PersonaBusquedaResponse?> BuscarPersonaAsync(string numId)
    {
        var persona = await _db.Personas
            .Include(p => p.TipoIdentificacion)
            .FirstOrDefaultAsync(p => p.Activo && p.NumeroIdentificacion == numId);

        if (persona is null) return null;

        return new PersonaBusquedaResponse
        {
            Id = persona.Id,
            Nombre = persona.Nombre,
            TipoId = persona.TipoIdentificacion?.Nombre ?? string.Empty,
            TipoIdentificacionId = persona.TipoIdentificacionId,
            NumeroIdentificacion = persona.NumeroIdentificacion,
            Empresa = persona.Empresa,
            Telefono = persona.Telefono,
            Email = persona.Email,
            TotalVisitas = persona.TotalVisitas,
            FechaUltimaVisita = persona.FechaUltimaVisita,
        };
    }

    public async Task<VisitanteResponse> RegistrarVisitanteAsync(
        CrearVisitanteRequest req, string ip)
    {
        var persona = await ObtenerOCrearPersonaAsync(
            req.TipoIdentificacionId,
            req.NumeroIdentificacion,
            req.Nombre,
            null,
            req.Telefono,
            req.Email);

        var registro = new RegistroVisitante
        {
            PersonaId = persona.Id,
            AreaId = req.AreaId,
            MotivoId = req.MotivoId,
            FechaEntrada = DateTime.UtcNow,
            GuardiaEntradaId = 1,
            EstadoAcceso = "Pendiente",
            ConsentimientoFirmado = req.ConsentimientoFirmado,
            Observaciones = req.Observaciones,
            IPSolicitud = ip,
            FechaCreacion = DateTime.UtcNow,
        };

        _db.RegistrosVisitantes.Add(registro);

        var solicitud = new SolicitudPendiente
        {
            TipoRegistro = "Visitante",
            RegistroId = 0,
            PersonaId = persona.Id,
            FechaSolicitud = DateTime.UtcNow,
            Estado = "Pendiente",
        };
        _db.SolicitudesPendientes.Add(solicitud);

        await _db.SaveChangesAsync();

        solicitud.RegistroId = registro.Id;
        await _db.SaveChangesAsync();

        await ActualizarContadorAsync(persona.Id);

        var area = await _db.Areas.FindAsync(req.AreaId);
        var motivo = await _db.MotivosVisita.FindAsync(req.MotivoId);

        // ── FIX 1: los nombres de campo deben coincidir EXACTAMENTE con
        //    la clase NuevaSolicitudEvent que la app móvil deserializa.
        await _hub.Clients.All.SendAsync("NuevaSolicitud", new NuevaSolicitudEvent(
            SolicitudId: solicitud.Id,
            RegistroId: registro.Id,
            TipoRegistro: "Visitante",
            NombrePersona: persona.Nombre,
            Empresa: persona.Empresa,
            NumeroIdentificacion: persona.NumeroIdentificacion,
            TipoID: persona.TipoIdentificacion?.Nombre ?? "",
            Motivo: motivo?.Nombre ?? "",
            Area: area?.Nombre ?? "",
            FechaSolicitud: registro.FechaEntrada
        ));

        await EnviarPushAGuardiasAsync(
            titulo: "Nueva solicitud — Visitante",
            cuerpo: $"{persona.Nombre} · {motivo?.Nombre}",
            solicitudId: solicitud.Id,
            tipoRegistro: "Visitante");

        _logger.LogInformation(
            "Visitante registrado: PersonaId={PersonaId}, RegistroId={RegistroId}",
            persona.Id, registro.Id);

        return new VisitanteResponse(
            registro.Id, persona.Id, persona.Nombre,
            area!.Nombre, motivo!.Nombre, registro.EstadoAcceso, registro.FechaEntrada,
            persona.TotalVisitas > 0,
            persona.TotalVisitas);
    }

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
            PersonaId = persona.Id,
            MotivoId = req.MotivoId,
            FechaEntrada = DateTime.UtcNow,
            UnidadPlacas = req.UnidadPlacas,
            FacturaRemision = req.FacturaRemision,
            GuardiaEntradaId = 1,
            EstadoAcceso = "Pendiente",
            ConsentimientoFirmado = req.ConsentimientoFirmado,
            Observaciones = req.Observaciones,
            IPSolicitud = ip,
            FechaCreacion = DateTime.UtcNow,
        };

        _db.RegistrosProveedores.Add(registro);

        var solicitud = new SolicitudPendiente
        {
            TipoRegistro = "Proveedor",
            RegistroId = 0,
            PersonaId = persona.Id,
            FechaSolicitud = DateTime.UtcNow,
            Estado = "Pendiente",
        };
        _db.SolicitudesPendientes.Add(solicitud);

        await _db.SaveChangesAsync();

        solicitud.RegistroId = registro.Id;
        await _db.SaveChangesAsync();

        await ActualizarContadorAsync(persona.Id);

        var motivo = await _db.MotivosVisita.FindAsync(req.MotivoId);

        // ── FIX 1: mismos nombres de campo que NuevaSolicitudEvent
        await _hub.Clients.All.SendAsync("NuevaSolicitud", new NuevaSolicitudEvent(
            SolicitudId: solicitud.Id,
            RegistroId: registro.Id,
            TipoRegistro: "Proveedor",
            NombrePersona: persona.Nombre,
            Empresa: persona.Empresa,
            NumeroIdentificacion: persona.NumeroIdentificacion,
            TipoID: persona.TipoIdentificacion?.Nombre ?? "",
            Motivo: motivo?.Nombre ?? "",
            Area: null,
            FechaSolicitud: registro.FechaEntrada
        ));

        await EnviarPushAGuardiasAsync(
            titulo: "Nueva solicitud — Proveedor",
            cuerpo: $"{persona.Nombre} · {persona.Empresa}",
            solicitudId: solicitud.Id,
            tipoRegistro: "Proveedor");

        _logger.LogInformation(
            "Proveedor registrado: PersonaId={PersonaId}, RegistroId={RegistroId}",
            persona.Id, registro.Id);

        return new ProveedorResponse(
            registro.Id, persona.Id, persona.Nombre,
            persona.Empresa ?? "", motivo!.Nombre, registro.EstadoAcceso, registro.FechaEntrada,
            persona.TotalVisitas > 0,
            persona.TotalVisitas);
    }

    public async Task<IEnumerable<SolicitudPendienteResponse>> ObtenerSolicitudesPendientesAsync()
    {
        var solicitudes = await _db.SolicitudesPendientes
            .Include(s => s.Persona).ThenInclude(p => p.TipoIdentificacion)
            .Where(s => s.Estado == "Pendiente")
            .ToListAsync();

        var respuestas = new List<SolicitudPendienteResponse>();

        foreach (var s in solicitudes)
        {
            string motivo = "";
            string area = "";
            string? placas = null;

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
                placas = reg?.UnidadPlacas;
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
                FechaSolicitud: s.FechaSolicitud,
                Placas: placas
            ));
        }

        return respuestas.OrderBy(r => r.FechaSolicitud);
    }

    public async Task<SolicitudPendienteResponse?> ObtenerSolicitudPorIdAsync(int solicitudId)
    {
        var s = await _db.SolicitudesPendientes
            .Include(x => x.Persona).ThenInclude(p => p.TipoIdentificacion)
            .FirstOrDefaultAsync(x => x.Id == solicitudId && x.Estado == "Pendiente");

        if (s == null) return null;

        string motivo = "";
        string area = "";
        string? placas = null;

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
            placas = reg?.UnidadPlacas;
        }

        return new SolicitudPendienteResponse(
            SolicitudId: s.Id,
            RegistroId: s.RegistroId,
            TipoRegistro: s.TipoRegistro,
            PersonaId: s.PersonaId,
            NombrePersona: s.Persona?.Nombre ?? "",
            Empresa: s.Persona?.Empresa,
            NumeroIdentificacion: s.Persona?.NumeroIdentificacion ?? "",
            TipoID: s.Persona?.TipoIdentificacion?.Nombre ?? "",
            Motivo: motivo,
            Area: area,
            FechaSolicitud: s.FechaSolicitud,
            Placas: placas
        );
    }

    public async Task<IEnumerable<AccesoActivoResponse>> ObtenerAccesosActivosAsync()
    {
        var respuestas = new List<AccesoActivoResponse>();
        var ahoraServidor = DateTime.UtcNow;

        var visitantes = await _db.RegistrosVisitantes
            .Include(r => r.Persona)
            .Include(r => r.Area)
            .Include(r => r.Gafete)
            .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
            .ToListAsync();
        respuestas.AddRange(visitantes.Select(v => new AccesoActivoResponse(
            RegistroId: v.Id,
            TipoRegistro: "Visitante",
            NombrePersona: v.Persona.Nombre,
            Empresa: v.Persona.Empresa,
            NumeroGafete: v.Gafete?.Codigo ?? "",       // <-- Código del gafete
            FechaEntrada: v.FechaEntrada,
            Area: v.Area.Nombre,
            MinutosLlevaDentro: (ahoraServidor - v.FechaEntrada).TotalMinutes
        )));

        var proveedores = await _db.RegistrosProveedores
            .Include(r => r.Persona)
            .Include(r => r.Gafete)
             .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
             .ToListAsync();
        respuestas.AddRange(proveedores.Select(p => new AccesoActivoResponse(
            RegistroId: p.Id,
            TipoRegistro: "Proveedor",
            NombrePersona: p.Persona.Nombre,
            Empresa: p.Persona.Empresa,
            NumeroGafete: p.Gafete?.Codigo ?? "",
            FechaEntrada: p.FechaEntrada,
            Area: "N/A",
            MinutosLlevaDentro: (ahoraServidor - p.FechaEntrada).TotalMinutes
        )));

        return respuestas.OrderByDescending(r => r.FechaEntrada);
    }

    public async Task<IEnumerable<AccesoActivoResponse>> ObtenerAccesosActivosZonaAsync()
    {
        var respuestas = new List<AccesoActivoResponse>();
        var ahoraServidor = DateTime.UtcNow;

        var visitantes = await _db.RegistrosVisitantes
            .Include(r => r.Persona)
            .Include(r => r.Area)
            .Include(r => r.Gafete)
            .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
            .ToListAsync();
        respuestas.AddRange(visitantes.Select(v => new AccesoActivoResponse(
            RegistroId: v.Id,
            TipoRegistro: "Visitante",
            NombrePersona: v.Persona.Nombre,
            Empresa: v.Persona.Empresa,
            NumeroGafete: v.Gafete?.Codigo ?? "",       // <-- Código del gafete
            FechaEntrada: v.FechaEntrada.Add(_offsetMexico),
            Area: v.Area.Nombre,
            MinutosLlevaDentro: (ahoraServidor - v.FechaEntrada).TotalMinutes
        )));

        var proveedores = await _db.RegistrosProveedores
            .Include(r => r.Persona)
            .Include(r => r.Gafete)
             .Where(r => r.EstadoAcceso == "Aprobado" && r.FechaSalida == null)
             .ToListAsync();
        respuestas.AddRange(proveedores.Select(p => new AccesoActivoResponse(
            RegistroId: p.Id,
            TipoRegistro: "Proveedor",
            NombrePersona: p.Persona.Nombre,
            Empresa: p.Persona.Empresa,
            NumeroGafete: p.Gafete?.Codigo ?? "",
            FechaEntrada: p.FechaEntrada.Add(_offsetMexico),
            Area: "N/A",
            MinutosLlevaDentro: (ahoraServidor - p.FechaEntrada).TotalMinutes
        )));

        return respuestas.OrderByDescending(r => r.FechaEntrada);
    }

    public async Task<bool> AprobarSolicitudAsync(AprobarSolicitudRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var solicitud = await _db.SolicitudesPendientes.FindAsync(request.SolicitudId);
            if (solicitud == null || solicitud.Estado != "Pendiente")
                return false;

            // Validar que el gafete exista, esté libre y activo
            var gafete = await _db.Gafetes.FirstOrDefaultAsync(g =>
                g.Id == request.GafeteId && g.Activo && g.Estado == "Libre");

            if (gafete == null)
                throw new InvalidOperationException("El gafete seleccionado no está disponible.");

            // Actualizar solicitud
            solicitud.Estado = "Aprobado";
            solicitud.GuardiaId = request.GuardiaId;

            // Actualizar registro según tipo
            if (solicitud.TipoRegistro == "Visitante")
            {
                var registro = await _db.RegistrosVisitantes.FindAsync(solicitud.RegistroId);
                if (registro != null)
                {
                    registro.EstadoAcceso = "Aprobado";
                    registro.GuardiaEntradaId = request.GuardiaId;
                    registro.GafeteId = request.GafeteId;        // <-- Asignar FK
                }
            }
            else if (solicitud.TipoRegistro == "Proveedor")
            {
                var registro = await _db.RegistrosProveedores.FindAsync(solicitud.RegistroId);
                if (registro != null)
                {
                    registro.EstadoAcceso = "Aprobado";
                    registro.GuardiaEntradaId = request.GuardiaId;
                    registro.GafeteId = request.GafeteId;
                }
            }

            // Cambiar estado del gafete a "En uso"
            gafete.Estado = "En uso";
            gafete.FechaModificacion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notificar por SignalR
            var guardia = await _db.Guardias.FindAsync(request.GuardiaId);
            await _hub.Clients.Group("Guardias").SendAsync("SolicitudResuelta", new SolicitudResueltaEvent(
                SolicitudId: request.SolicitudId,
                Estado: "Aprobado",
                NombreGuardia: guardia?.Nombre ?? ""
            ));

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al aprobar solicitud {SolicitudId}", request.SolicitudId);
            return false;
        }
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

        var guardia = await _db.Guardias.FindAsync(request.GuardiaId);

        // ── FIX 2: mismo nombre "SolicitudResuelta" — la app escucha solo este evento
        await _hub.Clients.All.SendAsync("SolicitudResuelta", new SolicitudResueltaEvent(
            SolicitudId: request.SolicitudId,
            Estado: "Rechazado",
            NombreGuardia: guardia?.Nombre ?? ""
        ));

        return true;
    }

    public async Task<bool> MarcarSalidaAsync(MarcarSalidaRequest request)
    {
        if (request.TipoRegistro == "Visitante")
        {
            var registro = await _db.RegistrosVisitantes.FindAsync(request.RegistroId);
            if (registro == null || registro.FechaSalida != null) return false;

            registro.FechaSalida = DateTime.UtcNow;
            registro.GuardiaSalidaId = request.GuardiaId;
        }
        else if (request.TipoRegistro == "Proveedor")
        {
            var registro = await _db.RegistrosProveedores.FindAsync(request.RegistroId);
            if (registro == null || registro.FechaSalida != null) return false;

            registro.FechaSalida = DateTime.UtcNow;
            registro.GuardiaSalidaId = request.GuardiaId;
        }
        else
        {
            return false;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> GuardarFcmTokenAsync(int guardiaId, string fcmToken)
    {
        var guardia = await _db.Guardias.FindAsync(guardiaId);
        if (guardia is null) return false;
        guardia.FcmToken = fcmToken;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task ActualizarContadorAsync(int personaId)
    {
        var persona = await _db.Personas.FindAsync(personaId);
        if (persona is null) return;
        persona.TotalVisitas++;
        persona.FechaUltimaVisita = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task EnviarPushAGuardiasAsync(
        string titulo, string cuerpo, int solicitudId, string tipoRegistro)
    {
        var tokens = await _db.Guardias
            .Where(g => g.Activo && g.FcmToken != null)
            .Select(g => g.FcmToken!)
            .ToListAsync();

        foreach (var token in tokens)
        {
            await _fcm.EnviarAsync(token, titulo, cuerpo, new Dictionary<string, string>
            {
                { "solicitudId", solicitudId.ToString() },
                { "tipoRegistro", tipoRegistro }
            });
        }
    }

    private async Task<Persona> ObtenerOCrearPersonaAsync(
        int tipoIdId, string numId, string nombre,
        string? empresa, string? telefono, string? email)
    {
        var persona = await _db.Personas
            .Include(p => p.TipoIdentificacion)
            .FirstOrDefaultAsync(p => p.NumeroIdentificacion == numId && p.Activo);

        if (persona is not null)
        {
            persona.Nombre = nombre;
            persona.Empresa = empresa ?? persona.Empresa;
            persona.Telefono = telefono ?? persona.Telefono;
            persona.Email = email ?? persona.Email;
            await _db.SaveChangesAsync();
            return persona;
        }

        persona = new Persona
        {
            TipoIdentificacionId = tipoIdId,
            NumeroIdentificacion = numId,
            Nombre = nombre,
            Empresa = empresa,
            Telefono = telefono,
            Email = email,
            Activo = true,
            FechaRegistro = DateTime.UtcNow,
        };
        _db.Personas.Add(persona);
        await _db.SaveChangesAsync();
        return persona;
    }

    public async Task<IEnumerable<GafeteDisponibleResponse>> ObtenerGafetesDisponiblesAsync()
    {
        return await _db.Gafetes
            .Where(g => g.Activo && g.Estado == "Libre")
            .Select(g => new GafeteDisponibleResponse(g.Id, g.Codigo))
            .ToListAsync();
    }
}
