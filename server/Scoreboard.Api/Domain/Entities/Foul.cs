namespace Scoreboard.Api.Domain.Entities;

public class Foul
{
    public long Id { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public int? PlayerId { get; set; }
    public string? Type { get; set; } // "personal", "técnica", etc.
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public Match Match { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public Player? Player { get; set; }
}
