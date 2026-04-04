using Microsoft.EntityFrameworkCore;
using RoclandAccesoControl.Web.Data;

namespace RoclandAccesoControl.Web.Services;

/// Background service que corre cada hora y cierra automáticamente
/// los accesos que llevan más de X horas sin registrar salida.
/// El umbral se configura en appsettings: AppSettings:AutoCerrarSalidaHoras

public class CierreAutomaticoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CierreAutomaticoService> _logger;

    public CierreAutomaticoService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<CierreAutomaticoService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CierreAutomaticoService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EjecutarCierreAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CierreAutomaticoService.");
            }

            // Ejecutar cada hora
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task EjecutarCierreAsync()
    {
        var horas = _config.GetValue<int>("AppSettings:AutoCerrarSalidaHoras", 24);
        var umbral = DateTime.UtcNow.AddHours(-horas);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoclandDbContext>();

        // Visitantes sin salida después del umbral
        var visitantes = await db.RegistrosVisitantes
            .Where(r => r.EstadoAcceso == "Aprobado"
                     && r.FechaSalida == null
                     && r.FechaEntrada <= umbral)
            .ToListAsync();

        foreach (var v in visitantes)
        {
            v.FechaSalida = v.FechaEntrada.AddHours(horas);
            v.EstadoAcceso = "Salido";
            v.Observaciones = (v.Observaciones ?? "") +
                $" [Salida automática por sistema a las {DateTime.UtcNow:HH:mm} UTC]";
        }

        // Proveedores sin salida después del umbral
        var proveedores = await db.RegistrosProveedores
            .Where(r => r.EstadoAcceso == "Aprobado"
                     && r.FechaSalida == null
                     && r.FechaEntrada <= umbral)
            .ToListAsync();

        foreach (var p in proveedores)
        {
            p.FechaSalida = p.FechaEntrada.AddHours(horas);
            p.EstadoAcceso = "Salido";
            p.Observaciones = (p.Observaciones ?? "") +
                $" [Salida automática por sistema a las {DateTime.UtcNow:HH:mm} UTC]";
        }

        // También cerrar solicitudes pendientes muy antiguas (más de 2 horas)
        var umbralSolicitudes = DateTime.UtcNow.AddHours(-2);
        var solicitudesViejas = await db.SolicitudesPendientes
            .Where(s => s.Estado == "Pendiente"
                     && s.FechaSolicitud <= umbralSolicitudes)
            .ToListAsync();

        foreach (var s in solicitudesViejas)
            s.Estado = "Expirado";

        var totalCerrados = visitantes.Count + proveedores.Count;
        var totalSolicitudes = solicitudesViejas.Count;

        if (totalCerrados > 0 || totalSolicitudes > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation(
                "Cierre automático: {Accesos} accesos cerrados, {Solicitudes} solicitudes expiradas.",
                totalCerrados, totalSolicitudes);
        }
    }
}