namespace Scoreboard.Api.Domain.Entities;

public class Match
{
    public int Id { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    // Scheduled | Live | Ended | Canceled | Suspended
    public string Status { get; set; } = "Scheduled";

    public int CurrentQuarter { get; set; } = 1;
    public int QuarterDurationSeconds { get; set; } = 600;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public DateTime? StartTimeUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    public ICollection<Quarter> Quarters { get; set; } = new List<Quarter>();
    public ICollection<ScoreEvent> ScoreEvents { get; set; } = new List<ScoreEvent>();
    public ICollection<Foul> Fouls { get; set; } = new List<Foul>();
    public TimerState? TimerState { get; set; }
}
