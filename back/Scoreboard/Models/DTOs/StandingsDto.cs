namespace Scoreboard.Models.DTOs;

// Fila para tabla de posiciones (si decides tipar la respuesta)
public record StandingsRowDto(int Id, string Name, string? Color, int Wins);
