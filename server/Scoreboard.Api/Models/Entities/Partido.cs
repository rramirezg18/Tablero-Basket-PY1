namespace Scoreboard.Api.Models.Entities;

public class Partido
{
    public int Id { get; set; }

    public int EquipoLocalId { get; set; }
    public int EquipoVisitanteId { get; set; }

    public string Estado { get; set; } = "Programado";

    // Duración del período en segundos (configurable por partido)
    public int DuracionPeriodoSegundos { get; set; } = 600;

    public int PuntajeLocal { get; set; }
    public int PuntajeVisitante { get; set; }

    // === Estado de juego mínimo en BD (sin runtime) ===
    public int Periodo { get; set; } = 1;        // 1..4
    public DateTime FechaPartido { get; set; } = DateTime.Now;

    // Navegación
    public Equipo EquipoLocal { get; set; } = null!;
    public Equipo EquipoVisitante { get; set; } = null!;

    public ICollection<EventoPuntaje> EventosPuntaje { get; set; } = new List<EventoPuntaje>();
}
