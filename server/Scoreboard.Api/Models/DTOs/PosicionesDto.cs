namespace Scoreboard.Api.Models.DTOs;

// Fila para tabla de posiciones (si decides tipar la respuesta)
public record FilaPosicionDto(int Id, string Nombre, string? Color, int Victorias);
