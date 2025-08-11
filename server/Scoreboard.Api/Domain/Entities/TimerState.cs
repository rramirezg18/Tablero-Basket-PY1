namespace Scoreboard.Api.Domain.Entities;

public class TimerState
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? QuarterEndsAtUtc { get; set; }
    public int RemainingSeconds { get; set; }
    public DateTime LastChangedUtc { get; set; } = DateTime.UtcNow;

    public Match Match { get; set; } = null!;
}
