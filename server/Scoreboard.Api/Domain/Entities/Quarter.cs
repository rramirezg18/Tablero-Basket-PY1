namespace Scoreboard.Api.Domain.Entities;

public class Quarter
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public byte Number { get; set; } // 1..4 (+ OT si quieres)

    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public int? HomeScoreAtEnd { get; set; }
    public int? AwayScoreAtEnd { get; set; }

    public Match Match { get; set; } = null!;
}
