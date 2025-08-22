namespace Scoreboard.Api.Models.Entities;

public class EventoPuntaje
{
    public long Id { get; set; }
    public int PartidoId { get; set; }
    public int EquipoId { get; set; }
    public int? JugadorId { get; set; }

    // -3,-2,-1,1,2,3 (negativos = ajustes/correcciones)
    public int Puntos { get; set; }

    public string? Nota { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public Partido Partido { get; set; } = null!;
    public Equipo Equipo { get; set; } = null!;
    public Jugador? Jugador { get; set; }
}
