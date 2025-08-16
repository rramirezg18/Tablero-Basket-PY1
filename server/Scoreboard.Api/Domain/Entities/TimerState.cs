namespace Scoreboard.Api.Domain.Entities;

public class TimerState
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public bool IsRunning { get; set; }
    public int RemainingSeconds { get; set; }
    public DateTime? QuarterEndsAtUtc { get; set; }
    public DateTime? LastChangedUtc { get; set; }

    // NUEVO: cuarto actual (1..4)
    public int CurrentQuarter { get; set; } = 1;
}
