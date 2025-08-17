using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Domain.Entities;
using Scoreboard.Api.Hubs;
using Scoreboard.Api.Infrastructure;

namespace Scoreboard.Api.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController(AppDbContext db, IHubContext<ScoreHub> hub) : ControllerBase
{
    // ===========================
    // GET estado del partido
    // ===========================
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var m = await db.Matches
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (m is null) return NotFound();

        // Timer snapshot (desde Match)
        int remaining;
        bool running;
        DateTime? endsAt;

        if (m.IsRunning && m.QuarterEndsAtUtc is not null)
        {
            remaining = Math.Max(0, (int)Math.Ceiling((m.QuarterEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds));
            running = true;
            endsAt = m.QuarterEndsAtUtc;
        }
        else
        {
            remaining = m.RemainingSeconds;
            running = false;
            endsAt = null;
        }

        // Conteo de faltas por equipo (totales del partido; ajusta si más adelante quieres por cuarto)
        var homeFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.HomeTeamId);
        var awayFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.AwayTeamId);

        return Ok(new
        {
            id = m.Id,
            homeTeamId = m.HomeTeamId,
            awayTeamId = m.AwayTeamId,
            homeTeam = m.HomeTeam.Name,
            awayTeam = m.AwayTeam.Name,
            homeScore = m.HomeScore,
            awayScore = m.AwayScore,
            status = m.Status,
            quarterDurationSeconds = m.QuarterDurationSeconds,
            quarter = m.CurrentQuarter,
            timer = new { running, remainingSeconds = remaining, quarterEndsAtUtc = endsAt },
            homeFouls,
            awayFouls
        });
    }

    // ===========================
    // NEW GAME
    // ===========================
    public record NewGameDto(string HomeName, string AwayName, int? QuarterDurationSeconds);

    [HttpPost("new")]
    public async Task<IActionResult> NewGame([FromBody] NewGameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HomeName) || string.IsNullOrWhiteSpace(dto.AwayName))
            return BadRequest("Team names required");

        var home = new Team { Name = dto.HomeName.Trim() };
        var away = new Team { Name = dto.AwayName.Trim() };
        db.AddRange(home, away);
        await db.SaveChangesAsync();

        var match = new Match
        {
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            Status = "Scheduled",
            QuarterDurationSeconds = dto.QuarterDurationSeconds is > 0 ? dto.QuarterDurationSeconds!.Value : 600,
            HomeScore = 0,
            AwayScore = 0,
            StartTimeUtc = null,
            CurrentQuarter = 1,
            IsRunning = false,
            RemainingSeconds = 0,
            QuarterEndsAtUtc = null
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return Ok(new
        {
            matchId = match.Id,
            homeTeamId = home.Id,
            awayTeamId = away.Id,
            quarterDurationSeconds = match.QuarterDurationSeconds
        });
    }

    // ===========================
    // NEW GAME BY TEAM IDs
    // ===========================
    public record NewGameByTeamsDto(int HomeTeamId, int AwayTeamId, int? QuarterDurationSeconds);

    [HttpPost("new-by-teams")]
    public async Task<IActionResult> NewByTeams([FromBody] NewGameByTeamsDto dto)
    {
        if (dto.HomeTeamId <= 0 || dto.AwayTeamId <= 0 || dto.HomeTeamId == dto.AwayTeamId)
            return BadRequest("Select two different teams");

        var home = await db.Teams.FindAsync(dto.HomeTeamId);
        var away = await db.Teams.FindAsync(dto.AwayTeamId);
        if (home is null || away is null) return BadRequest("Invalid team ids");

        var match = new Match
        {
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            Status = "Scheduled",
            QuarterDurationSeconds = dto.QuarterDurationSeconds is > 0 ? dto.QuarterDurationSeconds!.Value : 600,
            HomeScore = 0,
            AwayScore = 0,
            StartTimeUtc = null,
            CurrentQuarter = 1,
            IsRunning = false,
            RemainingSeconds = 0,
            QuarterEndsAtUtc = null
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return Ok(new
        {
            matchId = match.Id,
            homeTeamId = home.Id,
            awayTeamId = away.Id,
            quarterDurationSeconds = match.QuarterDurationSeconds
        });
    }

    // ===========================
    // SCORE (+1/+2/+3)
    // ===========================
    public record AddScoreDto(int TeamId, int Points);

    [HttpPost("{id}/score")]
    public async Task<IActionResult> AddScore(int id, [FromBody] AddScoreDto dto)
    {
        if (dto.Points is not (1 or 2 or 3)) return BadRequest("Points must be 1,2,3");

        var m = await db.Matches.FindAsync(id);
        if (m is null || m.Status != "Live") return NotFound();

        if (dto.TeamId == m.HomeTeamId) m.HomeScore += dto.Points;
        else if (dto.TeamId == m.AwayTeamId) m.AwayScore += dto.Points;
        else return BadRequest("Invalid teamId for this match");

        db.ScoreEvents.Add(new ScoreEvent { MatchId = id, TeamId = dto.TeamId, Points = dto.Points });
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("scoreUpdated", new { homeScore = m.HomeScore, awayScore = m.AwayScore });

        return Ok();
    }

    // ===========================
    // SCORE (ajuste -1, etc.)
    // ===========================
    public record AdjustScoreDto(int TeamId, int Delta);

    [HttpPost("{id}/score/adjust")]
    public async Task<IActionResult> AdjustScore(int id, [FromBody] AdjustScoreDto dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (dto.TeamId == m.HomeTeamId) m.HomeScore += dto.Delta;
        else if (dto.TeamId == m.AwayTeamId) m.AwayScore += dto.Delta;
        else return BadRequest("Invalid teamId for this match");

        db.ScoreEvents.Add(new ScoreEvent { MatchId = id, TeamId = dto.TeamId, Points = dto.Delta });
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("scoreUpdated", new { homeScore = m.HomeScore, awayScore = m.AwayScore });

        return Ok();
    }

    // ===========================
    // FOULS (sumar/restar)
    // ===========================
    public record AddFoulDto(int TeamId, int? PlayerId, string? Type);

    [HttpPost("{id}/fouls")]
    public async Task<IActionResult> AddFoul(int id, [FromBody] AddFoulDto dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        // Validar que el team pertenezca al partido
        if (dto.TeamId != m.HomeTeamId && dto.TeamId != m.AwayTeamId)
            return BadRequest("Invalid teamId for this match");

        db.Fouls.Add(new Foul
        {
            MatchId = id,
            TeamId = dto.TeamId,
            PlayerId = dto.PlayerId,
            Type = dto.Type,
            CreatedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Recalcular y emitir
        var homeFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.HomeTeamId);
        var awayFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.AwayTeamId);

        await hub.Clients.Group($"match-{id}")
            .SendAsync("foulsUpdated", new { homeFouls, awayFouls });

        return Ok(new { homeFouls, awayFouls });
    }

    public record AdjustFoulDto(int TeamId, int Delta);

    [HttpPost("{id}/fouls/adjust")]
    public async Task<IActionResult> AdjustFoul(int id, [FromBody] AdjustFoulDto dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (dto.TeamId != m.HomeTeamId && dto.TeamId != m.AwayTeamId)
            return BadRequest("Invalid teamId for this match");

        if (dto.Delta > 0)
        {
            for (int i = 0; i < dto.Delta; i++)
            {
                db.Fouls.Add(new Foul
                {
                    MatchId = id,
                    TeamId = dto.TeamId,
                    CreatedUtc = DateTime.UtcNow
                });
            }
        }
        else if (dto.Delta < 0)
        {
            var toRemove = await db.Fouls
                .Where(f => f.MatchId == id && f.TeamId == dto.TeamId)
                .OrderByDescending(f => f.Id)
                .Take(Math.Abs(dto.Delta))
                .ToListAsync();

            if (toRemove.Count == 0)
                return BadRequest("No fouls to remove");

            db.Fouls.RemoveRange(toRemove);
        }

        await db.SaveChangesAsync();

        var homeFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.HomeTeamId);
        var awayFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.AwayTeamId);

        await hub.Clients.Group($"match-{id}")
            .SendAsync("foulsUpdated", new { homeFouls, awayFouls });

        return Ok(new { homeFouls, awayFouls });
    }

    // ===========================
    // TIMER START/PAUSE/RESUME/RESET
    // ===========================
    public record StartTimerDto(int? QuarterDurationSeconds);

    [HttpPost("{id}/timer/start")]
    public async Task<IActionResult> StartTimer(int id, [FromBody] StartTimerDto? dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (dto?.QuarterDurationSeconds is int q && q > 0) m.QuarterDurationSeconds = q;

        m.RemainingSeconds = m.QuarterDurationSeconds;
        m.IsRunning = true;
        m.QuarterEndsAtUtc = DateTime.UtcNow.AddSeconds(m.RemainingSeconds);
        m.Status = "Live";
        m.StartTimeUtc ??= DateTime.UtcNow;

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerStarted", new { quarterEndsAtUtc = m.QuarterEndsAtUtc, remainingSeconds = m.RemainingSeconds });

        await hub.Clients.Group($"match-{id}")
          .SendAsync("quarterChanged", new { quarter = m.CurrentQuarter });
        await hub.Clients.Group($"match-{id}")
          .SendAsync("buzzer", new { reason = "quarter-start" });

        return Ok(new { remainingSeconds = m.RemainingSeconds, quarterEndsAtUtc = m.QuarterEndsAtUtc });
    }

    [HttpPost("{id}/timer/pause")]
    public async Task<IActionResult> PauseTimer(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (m.IsRunning && m.QuarterEndsAtUtc is not null)
            m.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((m.QuarterEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds));

        m.IsRunning = false;
        m.QuarterEndsAtUtc = null;

        await db.SaveChangesAsync();
        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerPaused", new { remainingSeconds = m.RemainingSeconds });

        return Ok(new { m.RemainingSeconds });
    }

    [HttpPost("{id}/timer/resume")]
    public async Task<IActionResult> ResumeTimer(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();
        if (m.RemainingSeconds <= 0) return BadRequest("Nothing to resume");

        m.IsRunning = true;
        m.QuarterEndsAtUtc = DateTime.UtcNow.AddSeconds(m.RemainingSeconds);

        await db.SaveChangesAsync();
        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerResumed", new { quarterEndsAtUtc = m.QuarterEndsAtUtc, remainingSeconds = m.RemainingSeconds });

        return Ok(new { remainingSeconds = m.RemainingSeconds, quarterEndsAtUtc = m.QuarterEndsAtUtc });
    }

    [HttpPost("{id}/timer/reset")]
    public async Task<IActionResult> ResetTimer(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        m.IsRunning = false;
        m.RemainingSeconds = 0;
        m.QuarterEndsAtUtc = null;

        await db.SaveChangesAsync();
        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerReset", new { remainingSeconds = m.RemainingSeconds });

        return Ok(new { m.RemainingSeconds });
    }

    // ===========================
    // Avanzar periodo (Quarter)
    // ===========================
    [HttpPost("{id}/quarters/advance")]
    public async Task<IActionResult> AdvanceQuarter(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (m.CurrentQuarter < 4)
            m.CurrentQuarter += 1;
        else
            m.Status = "Finished";

        m.IsRunning = false;
        m.RemainingSeconds = 0;
        m.QuarterEndsAtUtc = null;

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = m.CurrentQuarter });
        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-end" });

        return Ok(new { quarter = m.CurrentQuarter });
    }

    [HttpPost("{id}/quarters/auto-advance")]
    public async Task<IActionResult> AutoAdvanceQuarter(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        var remaining = m.IsRunning && m.QuarterEndsAtUtc is not null
            ? Math.Max(0, (int)Math.Ceiling((m.QuarterEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds))
            : m.RemainingSeconds;

        if (remaining > 0)
            return BadRequest("Quarter not finished");

        if (m.CurrentQuarter < 4)
            m.CurrentQuarter += 1;
        else
            m.Status = "Finished";

        m.IsRunning = false;
        m.RemainingSeconds = 0;
        m.QuarterEndsAtUtc = null;

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = m.CurrentQuarter });
        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-end" });

        if (m.Status == "Finished")
        {
            await hub.Clients.Group($"match-{id}")
                .SendAsync("gameEnded", new
                {
                    home = m.HomeScore,
                    away = m.AwayScore,
                    winner = m.HomeScore == m.AwayScore ? "draw" : (m.HomeScore > m.AwayScore ? "home" : "away")
                });
        }

        return Ok(new { quarter = m.CurrentQuarter });
    }
}
