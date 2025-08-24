namespace Scoreboard.Models.Entities;

public class Foul
{
    public long Id { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public int? PlayerId { get; set; }

    public string? Type { get; set; }
    public DateTime DateRegister { get; set; } = DateTime.Now;

    public Match Match { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public Player? Player { get; set; }
}
