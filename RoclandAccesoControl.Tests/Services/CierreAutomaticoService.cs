using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RoclandAccesoControl.Tests.Helpers;
using RoclandAccesoControl.Web.Data;
using RoclandAccesoControl.Web.Models.Entities;
using RoclandAccesoControl.Web.Services;

namespace RoclandAccesoControl.Tests.Services;

public class CierreAutomaticoTests
{
    [Fact]
    public async Task CierreAutomatico_CierraAccesosSinSalidaAntiguos()
    {
        // Arrange
        var ctx = DbContextHelper.CrearContexto("TestCierre");

        // Insertar un registro muy antiguo (más de 24 horas, sin salida)
        ctx.RegistrosVisitantes.Add(new RegistroVisitante
        {
            PersonaId = 1,
            AreaId = 1,
            MotivoId = 1,
            FechaEntrada = DateTime.UtcNow.AddHours(-25), // ← antiguo
            GuardiaEntradaId = 1,
            EstadoAcceso = "Aprobado",
            ConsentimientoFirmado = true
        });

        // Insertar registro reciente (menos de 24h, no debe cerrarse)
        ctx.RegistrosVisitantes.Add(new RegistroVisitante
        {
            PersonaId = 1,
            AreaId = 1,
            MotivoId = 1,
            FechaEntrada = DateTime.UtcNow.AddHours(-2), // ← reciente
            GuardiaEntradaId = 1,
            EstadoAcceso = "Aprobado",
            ConsentimientoFirmado = true
        });

        // Agregar persona necesaria
        ctx.Personas.Add(new Persona
        {
            Id = 1,
            Nombre = "Test Cierre",
            TipoIdentificacionId = 1,
            NumeroIdentificacion = "CIERRE001"
        });

        await ctx.SaveChangesAsync();

        // Crear el servicio con scope factory simulado
        var services = new ServiceCollection();
        services.AddSingleton(ctx);
        services.AddScoped<RoclandDbContext>(_ => ctx);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:AutoCerrarSalidaHoras"] = "24"
            }).Build();

        var logger = new Mock<ILogger<CierreAutomaticoService>>();
        var svc = new CierreAutomaticoService(scopeFactory, config, logger.Object);

        // Invocar el método privado a través de reflexión para testing
        var method = typeof(CierreAutomaticoService)
            .GetMethod("EjecutarCierreAsync",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(svc, null)!;

        // Assert
        var registros = await ctx.RegistrosVisitantes.ToListAsync();
        var antiguo = registros.First(r => r.FechaEntrada < DateTime.UtcNow.AddHours(-10));
        var reciente = registros.First(r => r.FechaEntrada > DateTime.UtcNow.AddHours(-10));

        antiguo.EstadoAcceso.Should().Be("Salido");
        antiguo.FechaSalida.Should().NotBeNull();
        antiguo.Observaciones.Should().Contain("automática");

        reciente.EstadoAcceso.Should().Be("Aprobado");
        reciente.FechaSalida.Should().BeNull();
    }
}