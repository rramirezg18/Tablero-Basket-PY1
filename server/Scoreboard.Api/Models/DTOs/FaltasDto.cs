namespace Scoreboard.Api.Models.DTOs;

public record AgregarFaltaDto(int EquipoId, int? JugadorId, string? Tipo);
public record AjustarFaltaDto(int EquipoId, int Delta);
