using Microsoft.EntityFrameworkCore;
using RoclandAccesoControl.Web.Models.Entities;

namespace RoclandAccesoControl.Web.Data;

public class RoclandDbContext : DbContext
{
    public RoclandDbContext(DbContextOptions<RoclandDbContext> options) : base(options) { }

    public DbSet<TipoIdentificacion> TiposIdentificacion => Set<TipoIdentificacion>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<MotivoVisita> MotivosVisita => Set<MotivoVisita>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Guardia> Guardias => Set<Guardia>();
    public DbSet<RegistroVisitante> RegistrosVisitantes => Set<RegistroVisitante>();
    public DbSet<RegistroProveedor> RegistrosProveedores => Set<RegistroProveedor>();
    public DbSet<SolicitudPendiente> SolicitudesPendientes => Set<SolicitudPendiente>();
    public DbSet<Administrador> Administradores => Set<Administrador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Nombres de tablas ──────────────────────────────────────────
        modelBuilder.Entity<TipoIdentificacion>()
            .ToTable("TBL_ROCLAND_GUARD_TIPODEIDENTIFICACION");
        modelBuilder.Entity<Area>()
            .ToTable("TBL_ROCLAND_GUARD_AREAS");
        modelBuilder.Entity<MotivoVisita>()
            .ToTable("TBL_ROCLAND_GUARD_MOTIVOVISITA");
        modelBuilder.Entity<Persona>()
            .ToTable("TBL_ROCLAND_GUARD_PERSONAS");
        modelBuilder.Entity<Guardia>()
            .ToTable("TBL_ROCLAND_GUARD_GUARDIAS");
        modelBuilder.Entity<RegistroVisitante>()
            .ToTable("TBL_ROCLAND_GUARD_REGISTROVISITANTES");
        modelBuilder.Entity<RegistroProveedor>()
            .ToTable("TBL_ROCLAND_GUARD_REGISTROPROVEEDORES");
        modelBuilder.Entity<SolicitudPendiente>()
            .ToTable("TBL_ROCLAND_GUARD_SOLICITUDESPENDIENTES");
        modelBuilder.Entity<Administrador>()
            .ToTable("TBL_ROCLAND_GUARD_ADMINISTRADORES");

        // ── Índice único Persona ───────────────────────────────────────
        modelBuilder.Entity<Persona>()
            .HasIndex(p => new { p.TipoIdentificacionId, p.NumeroIdentificacion })
            .IsUnique()
            .HasDatabaseName("UQ_PERSONA");

        // ── Columnas calculadas (solo lectura, generadas en BD) ────────
        modelBuilder.Entity<RegistroVisitante>()
            .Property(r => r.HoraEntrada).ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<RegistroVisitante>()
            .Property(r => r.HoraSalida).ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<RegistroVisitante>()
            .Property(r => r.MinutosEstancia).ValueGeneratedOnAddOrUpdate();

        modelBuilder.Entity<RegistroProveedor>()
            .Property(r => r.HoraEntrada).ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<RegistroProveedor>()
            .Property(r => r.HoraSalida).ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<RegistroProveedor>()
            .Property(r => r.MinutosEstancia).ValueGeneratedOnAddOrUpdate();

        // ── Relaciones Guardia (dos FK a la misma tabla) ───────────────
        modelBuilder.Entity<RegistroVisitante>()
            .HasOne(r => r.GuardiaEntrada)
            .WithMany(g => g.EntradasAutorizadas)
            .HasForeignKey(r => r.GuardiaEntradaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RegistroVisitante>()
            .HasOne(r => r.GuardiaSalida)
            .WithMany(g => g.SalidasAutorizadas)
            .HasForeignKey(r => r.GuardiaSalidaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RegistroProveedor>()
            .HasOne(r => r.GuardiaEntrada)
            .WithMany(g => g.EntradasProvAutorizadas)
            .HasForeignKey(r => r.GuardiaEntradaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RegistroProveedor>()
            .HasOne(r => r.GuardiaSalida)
            .WithMany(g => g.SalidasProvAutorizadas)
            .HasForeignKey(r => r.GuardiaSalidaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
