namespace Scoreboard.Api.Models.Entities;

public class Falta
{
    public long Id { get; set; }
    public int PartidoId { get; set; }
    public int EquipoId { get; set; }
    public int? JugadorId { get; set; }

    public string? Tipo { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public Partido Partido { get; set; } = null!;
    public Equipo Equipo { get; set; } = null!;
    public Jugador? Jugador { get; set; }
}
