namespace Scoreboard.Models.Entities;

public class TeamWin
{
    public long Id { get; set; }

    public int TeamId { get; set; }
    public int MatchId { get; set; }

    public DateTime DateRegistered { get; set; } = DateTime.Now;

    public Team Team { get; set; } = null!;
    public Match Match { get; set; } = null!;
}
