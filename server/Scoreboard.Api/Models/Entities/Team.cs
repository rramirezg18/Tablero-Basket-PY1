namespace Scoreboard.Api.Models.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; // mantenido

    public ICollection<Player> Players { get; set; } = new List<Player>();
}
