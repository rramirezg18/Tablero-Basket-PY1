namespace Scoreboard.Api.Models.Entities;

public class Jugador
{
    public int Id { get; set; }
    public int EquipoId { get; set; }
    public int? Numero { get; set; }
    public string Nombre { get; set; } = null!;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public Equipo Equipo { get; set; } = null!;
}
