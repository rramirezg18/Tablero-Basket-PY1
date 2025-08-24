namespace Scoreboard.Models.Entities;

public class Player
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int? Number { get; set; }
    public string Name { get; set; } = null!;
    public DateTime DateRegister { get; set; } = DateTime.Now;

    public Team Team { get; set; } = null!;
}
