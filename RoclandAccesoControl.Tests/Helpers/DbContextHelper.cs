using Microsoft.EntityFrameworkCore;
using RoclandAccesoControl.Web.Data;
using RoclandAccesoControl.Web.Models.Entities;

namespace RoclandAccesoControl.Tests.Helpers;

public static class DbContextHelper
{
    public static RoclandDbContext CrearContexto(string nombre = "TestDb")
    {
        var options = new DbContextOptionsBuilder<RoclandDbContext>()
            .UseInMemoryDatabase(databaseName: nombre + Guid.NewGuid())
            .Options;

        var ctx = new RoclandDbContext(options);
        SembrarDatos(ctx);
        return ctx;
    }

    private static void SembrarDatos(RoclandDbContext ctx)
    {
        // Tipos de identificación
        ctx.TiposIdentificacion.AddRange(
            new TipoIdentificacion { Id = 1, Nombre = "INE / IFE", Activo = true },
            new TipoIdentificacion { Id = 2, Nombre = "Pasaporte", Activo = true }
        );

        // Áreas
        ctx.Areas.AddRange(
            new Area { Id = 1, Nombre = "Recepción", Activo = true },
            new Area { Id = 2, Nombre = "Gerencia", Activo = true }
        );

        // Motivos
        ctx.MotivosVisita.AddRange(
            new MotivoVisita { Id = 1, Nombre = "Reunión de trabajo", Activo = true },
            new MotivoVisita { Id = 2, Nombre = "Entrega de mercancía", Activo = true }
        );

        // Guardia de prueba
        ctx.Guardias.Add(new Guardia
        {
            Id = 1,
            Nombre = "Guardia Test",
            Usuario = "guardia_test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        // Admin de prueba
        ctx.Administradores.Add(new Administrador
        {
            Id = 1,
            Nombre = "Admin Test",
            Usuario = "admin_test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Rol = "Admin",
            Activo = true
        });

        ctx.SaveChanges();
    }
}