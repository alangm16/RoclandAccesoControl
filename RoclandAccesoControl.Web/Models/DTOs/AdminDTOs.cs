namespace RoclandAccesoControl.Web.Models.DTOs;

// ── KPIs Dashboard ─────────────────────────────────────────────────────
public record DashboardKpiDto(
    int DentroAhora,
    int TotalHoy,
    int TotalVisitantesHoy,
    int TotalProveedoresHoy,
    double PromedioEstanciaMinutos,
    int SolicitudesPendientes
);

// ── Flujo por hora ─────────────────────────────────────────────────────
public record FlujoPorHoraDto(int Hora, int Total);

// ── Flujo diario del mes ───────────────────────────────────────────────
public record FlujoDiarioDto(string Fecha, int Visitantes, int Proveedores);

// ── Área más visitada ──────────────────────────────────────────────────
public record AreaVisitadaDto(string Area, int Total);

// ── Acceso en historial ────────────────────────────────────────────────
public record HistorialAccesoDto(
    int Id,
    string Tipo,
    string Nombre,
    string? Empresa,
    string NumeroIdentificacion,
    string? Area,
    string Motivo,
    DateTime FechaEntrada,
    DateTime? FechaSalida,
    int? MinutosEstancia,
    string EstadoAcceso,
    string? NumeroGafete,
    string Guardia
);

// ── Perfil de persona ──────────────────────────────────────────────────
public record PersonaPerfilDto(
    int Id,
    string Nombre,
    string TipoID,
    string NumeroIdentificacion,
    string? Empresa,
    string? Telefono,
    string? Email,
    int TotalVisitas,
    DateTime FechaRegistro,
    DateTime? FechaUltimaVisita
);

// ── CRUD catálogos ─────────────────────────────────────────────────────
public record CatalogoCreateDto(string Nombre);
public record GuardiaCreateDto(string Nombre, string Usuario, string Password);
public record GuardiaUpdateDto(string Nombre, bool Activo);
public record AdminCreateDto(string Nombre, string Usuario, string Password, string Rol);