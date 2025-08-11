using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Domain.Entities;

namespace Scoreboard.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Quarter> Quarters => Set<Quarter>();
    public DbSet<ScoreEvent> ScoreEvents => Set<ScoreEvent>();
    public DbSet<Foul> Fouls => Set<Foul>();
    public DbSet<TimerState> TimerStates => Set<TimerState>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Team
        b.Entity<Team>()
            .HasIndex(t => t.Name).IsUnique();

        // Player
        b.Entity<Player>()
            .HasOne(p => p.Team).WithMany(t => t.Players)
            .HasForeignKey(p => p.TeamId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Player>()
            .HasIndex(p => new { p.TeamId, p.Number }).IsUnique()
            .HasFilter("[Number] IS NOT NULL");

        // Match
        b.Entity<Match>()
            .HasOne(m => m.HomeTeam).WithMany()
            .HasForeignKey(m => m.HomeTeamId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Match>()
            .HasOne(m => m.AwayTeam).WithMany()
            .HasForeignKey(m => m.AwayTeamId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Match>()
            .HasIndex(m => m.Status);
        b.Entity<Match>()
            .Property(m => m.Status).HasMaxLength(16);

        // Quarter
        b.Entity<Quarter>()
            .HasOne(q => q.Match).WithMany(m => m.Quarters)
            .HasForeignKey(q => q.MatchId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Quarter>()
            .HasIndex(q => new { q.MatchId, q.Number }).IsUnique();

        // ScoreEvent
        b.Entity<ScoreEvent>()
            .HasOne(se => se.Match).WithMany(m => m.ScoreEvents)
            .HasForeignKey(se => se.MatchId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ScoreEvent>()
            .HasOne(se => se.Team).WithMany()
            .HasForeignKey(se => se.TeamId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ScoreEvent>()
            .HasOne(se => se.Player).WithMany()
            .HasForeignKey(se => se.PlayerId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ScoreEvent>()
            .HasIndex(se => se.MatchId);
        b.Entity<ScoreEvent>()
            .HasIndex(se => se.CreatedUtc);

        // Foul
        b.Entity<Foul>()
            .HasOne(f => f.Match).WithMany(m => m.Fouls)
            .HasForeignKey(f => f.MatchId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Foul>()
            .HasOne(f => f.Team).WithMany()
            .HasForeignKey(f => f.TeamId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Foul>()
            .HasOne(f => f.Player).WithMany()
            .HasForeignKey(f => f.PlayerId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Foul>()
            .HasIndex(f => new { f.MatchId, f.TeamId });

        // TimerState
        b.Entity<TimerState>()
            .HasOne(ts => ts.Match).WithOne(m => m.TimerState!)
            .HasForeignKey<TimerState>(ts => ts.MatchId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TimerState>()
            .HasIndex(ts => ts.MatchId).IsUnique();

        base.OnModelCreating(b);
    }
}
