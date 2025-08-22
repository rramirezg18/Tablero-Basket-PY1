using Microsoft.EntityFrameworkCore;

// ⬇️ Alias locales: TODOS apuntan a Models.Entities
using Equipo         = Scoreboard.Api.Models.Entities.Equipo;
using Jugador        = Scoreboard.Api.Models.Entities.Jugador;
using Partido        = Scoreboard.Api.Models.Entities.Partido;
using EventoPuntaje  = Scoreboard.Api.Models.Entities.EventoPuntaje;
using Falta          = Scoreboard.Api.Models.Entities.Falta;
using VictoriaEquipo = Scoreboard.Api.Models.Entities.VictoriaEquipo;

namespace Scoreboard.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Equipo>         Equipos         => Set<Equipo>();
    public DbSet<Jugador>        Jugadores       => Set<Jugador>();
    public DbSet<Partido>        Partidos        => Set<Partido>();
    public DbSet<EventoPuntaje>  EventosPuntaje  => Set<EventoPuntaje>();
    public DbSet<Falta>          Faltas          => Set<Falta>();
    public DbSet<VictoriaEquipo> VictoriasEquipo => Set<VictoriaEquipo>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Equipo
        b.Entity<Equipo>()
            .HasIndex(t => t.Nombre).IsUnique();

        // Jugador
        b.Entity<Jugador>()
            .HasOne(p => p.Equipo).WithMany(t => t.Jugadores)
            .HasForeignKey(p => p.EquipoId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Jugador>()
            .HasIndex(p => new { p.EquipoId, p.Numero }).IsUnique()
            .HasFilter("[Numero] IS NOT NULL");

        // Partido
        b.Entity<Partido>()
            .HasOne(m => m.EquipoLocal).WithMany()
            .HasForeignKey(m => m.EquipoLocalId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Partido>()
            .HasOne(m => m.EquipoVisitante).WithMany()
            .HasForeignKey(m => m.EquipoVisitanteId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Partido>()
            .HasIndex(m => m.Estado);
        b.Entity<Partido>()
            .Property(m => m.Estado).HasMaxLength(16);

        // EventoPuntaje
        b.Entity<EventoPuntaje>()
            .HasOne(se => se.Partido).WithMany(m => m.EventosPuntaje)
            .HasForeignKey(se => se.PartidoId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<EventoPuntaje>()
            .HasOne(se => se.Equipo).WithMany()
            .HasForeignKey(se => se.EquipoId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<EventoPuntaje>()
            .HasOne(se => se.Jugador).WithMany()
            .HasForeignKey(se => se.JugadorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<EventoPuntaje>()
            .HasIndex(se => se.PartidoId);
        b.Entity<EventoPuntaje>()
            .HasIndex(se => se.FechaRegistro);

        // Falta
        b.Entity<Falta>()
            .HasOne(f => f.Partido).WithMany()
            .HasForeignKey(f => f.PartidoId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Falta>()
            .HasOne(f => f.Equipo).WithMany()
            .HasForeignKey(f => f.EquipoId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Falta>()
            .HasOne(f => f.Jugador).WithMany()
            .HasForeignKey(f => f.JugadorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Falta>()
            .HasIndex(f => new { f.PartidoId, f.EquipoId });

        // VictoriaEquipo
        b.Entity<VictoriaEquipo>()
            .HasOne(tw => tw.Equipo).WithMany()
            .HasForeignKey(tw => tw.EquipoId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<VictoriaEquipo>()
            .HasOne(tw => tw.Partido).WithMany()
            .HasForeignKey(tw => tw.PartidoId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<VictoriaEquipo>()
            .HasIndex(tw => new { tw.EquipoId, tw.PartidoId }).IsUnique();
        b.Entity<VictoriaEquipo>()
            .HasIndex(tw => tw.EquipoId);

        base.OnModelCreating(b);
    }
}
