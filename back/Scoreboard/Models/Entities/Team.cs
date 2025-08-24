namespace Scoreboard.Models.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;

    public ICollection<Player> Players { get; set; } = new List<Player>();
}
