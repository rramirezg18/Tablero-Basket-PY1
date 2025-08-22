namespace Scoreboard.Api.Models.DTOs;

public record AgregarPuntajeDto(int EquipoId, int Puntos);
public record AjustarPuntajeDto(int EquipoId, int Delta);
