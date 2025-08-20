namespace Scoreboard.Api.Domain.Entities;

public class Match
{
    public int Id { get; set; }

    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    public string Status { get; set; } = "Scheduled";

    // Duración del período en segundos (configurable por partido)
    public int QuarterDurationSeconds { get; set; } = 600;

    public int HomeScore { get; set; }
    public int AwayScore { get; set; }

    // 👇 Renombrado (antes CurrentQuarter)
    public int Period { get; set; } = 1;      // 1..4

    // 👇 Renombrados y/o eliminados de BD
    // Se sacan del modelo persistente:
    //  - IsRunning            (eliminado de BD)
    //  - RemainingSeconds     (eliminado de BD)
    //  - PeriodEnd            (eliminado de BD)
    //  - StartMatch           (eliminado de BD)

    // 👇 Renombrado (antes CreatedUtc)
    public DateTime DateMatch { get; set; } = DateTime.Now;

    // Navegación
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    public ICollection<ScoreEvent> ScoreEvents { get; set; } = new List<ScoreEvent>();
}
