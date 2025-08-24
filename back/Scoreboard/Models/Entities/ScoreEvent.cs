namespace Scoreboard.Models.Entities;

public class ScoreEvent
{
    public long Id { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public int? PlayerId { get; set; }

    // -3,-2,-1,1,2,3 (negativos = ajustes/correcciones)
    public int Points { get; set; }

    public string? Note { get; set; }
    public DateTime DateRegister { get; set; } = DateTime.Now;

    public Match Match { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public Player? Player { get; set; }
}
