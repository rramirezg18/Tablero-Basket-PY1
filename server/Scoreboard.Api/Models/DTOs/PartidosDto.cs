namespace Scoreboard.Api.Models.DTOs;

public record NuevoPartidoDto(string NombreLocal, string NombreVisitante, int? DuracionPeriodoSegundos);
public record NuevoPartidoPorEquiposDto(int EquipoLocalId, int EquipoVisitanteId, int? DuracionPeriodoSegundos);
