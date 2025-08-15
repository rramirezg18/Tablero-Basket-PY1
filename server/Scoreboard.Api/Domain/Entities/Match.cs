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

    public DateTime? StartTimeUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    public ICollection<ScoreEvent> ScoreEvents { get; set; } = new List<ScoreEvent>();
}
