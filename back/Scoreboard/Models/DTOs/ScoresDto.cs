namespace Scoreboard.Models.DTOs;

public record AddScoreDto(int TeamId, int Points);
public record AdjustScoreDto(int TeamId, int Delta);
