using System;

namespace Scoreboard.Api.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int? Number { get; set; }
    public string Name { get; set; } = null!;
    public DateTime DateRegister { get; set; } = DateTime.UtcNow; // antes CreatedUtc

    public Team Team { get; set; } = null!;
}
