//name
namespace Scoreboard.Models.DTOs;

public record NewGameDto(string HomeName, string AwayName, int? QuarterDurationSeconds);
public record NewGameByTeamsDto(int HomeTeamId, int AwayTeamId, int? QuarterDurationSeconds);
