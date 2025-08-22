namespace Scoreboard.Api.Models.Entities;

public class Equipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Color { get; set; }
    public DateTime FechaCreadoUtc { get; set; } = DateTime.UtcNow; // mantenido

    public ICollection<Jugador> Jugadores { get; set; } = new List<Jugador>();
}
