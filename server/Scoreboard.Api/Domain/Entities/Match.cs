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

    // ===== Estado del reloj/cuarto ahora en Match (antes en TimerState) =====
    public int CurrentQuarter { get; set; } = 1;      // 1..4
    public bool IsRunning { get; set; } = false;
    public int RemainingSeconds { get; set; } = 0;
    public DateTime? QuarterEndsAtUtc { get; set; }

    // Metadatos existentes
    public DateTime? StartTimeUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navegación
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    // Si no usas Quarters/Fouls como navegación, puedes dejarlas fuera.
    public ICollection<ScoreEvent> ScoreEvents { get; set; } = new List<ScoreEvent>();
}
