namespace Scoreboard.Models.DTOs;

public record AddFoulDto(int TeamId, int? PlayerId, string? Type);
public record AdjustFoulDto(int TeamId, int Delta);
