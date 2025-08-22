namespace Scoreboard.Api.Models.Entities;

public class VictoriaEquipo
{
    public long Id { get; set; }

    public int EquipoId { get; set; }
    public int PartidoId { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public Equipo Equipo { get; set; } = null!;
    public Partido Partido { get; set; } = null!;
}
