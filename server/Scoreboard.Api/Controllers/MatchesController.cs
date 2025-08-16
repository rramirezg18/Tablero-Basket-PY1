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

        var ts = await db.TimerStates.SingleOrDefaultAsync(t => t.MatchId == id);
        int remaining = 0; bool running = false; DateTime? endsAt = null;
        if (ts is not null)
        {
            running = ts.IsRunning;
            remaining = ts.IsRunning && ts.QuarterEndsAtUtc is not null
                ? Math.Max(0, (int)Math.Ceiling((ts.QuarterEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds))
                : ts.RemainingSeconds;
            endsAt = ts.QuarterEndsAtUtc;
        }

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
            quarter = ts?.CurrentQuarter ?? 1, // ← NUEVO
            timer = new { running, remainingSeconds = remaining, quarterEndsAtUtc = endsAt }
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
            //CurrentQuarter = 1,
            QuarterDurationSeconds = dto.QuarterDurationSeconds is > 0 ? dto.QuarterDurationSeconds!.Value : 600,
            HomeScore = 0,
            AwayScore = 0,
            StartTimeUtc = null
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        // Timer a cero (1 por partido)
        var ts = new TimerState
        {
            MatchId = match.Id,
            IsRunning = false,
            RemainingSeconds = 0,
            QuarterEndsAtUtc = null,
            CurrentQuarter = 1 // ← NUEVO (explícito)
        };
        db.TimerStates.Add(ts);
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
    // TIMER START/PAUSE/RESUME/RESET
    // ===========================
    public record StartTimerDto(int? QuarterDurationSeconds);

    [HttpPost("{id}/timer/start")]
    public async Task<IActionResult> StartTimer(int id, [FromBody] StartTimerDto? dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (dto?.QuarterDurationSeconds is int q && q > 0) m.QuarterDurationSeconds = q;

        var ts = await db.TimerStates.SingleOrDefaultAsync(t => t.MatchId == id)
                 ?? new TimerState { MatchId = id, CurrentQuarter = 1 };

        ts.RemainingSeconds = m.QuarterDurationSeconds;
        ts.IsRunning = true;
        ts.LastChangedUtc = DateTime.UtcNow;
        ts.QuarterEndsAtUtc = DateTime.UtcNow.AddSeconds(ts.RemainingSeconds);

        m.Status = "Live";
        m.StartTimeUtc ??= DateTime.UtcNow;

        db.Update(m);
        db.Update(ts);
        await db.SaveChangesAsync();

        // Notifica inicio de conteo
        await hub.Clients.Group($"match-{id}")
            .SendAsync("timerStarted", new { quarterEndsAtUtc = ts.QuarterEndsAtUtc, remainingSeconds = ts.RemainingSeconds });

        // 🔔 NUEVO: anuncia el cuarto actual y buzzer de INICIO
        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = ts.CurrentQuarter });
        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-start" });

        return Ok(new { remainingSeconds = ts.RemainingSeconds, quarterEndsAtUtc = ts.QuarterEndsAtUtc });
    }

    [HttpPost("{id}/timer/pause")]
    public async Task<IActionResult> PauseTimer(int id)
    {
        var ts = await db.TimerStates.SingleOrDefaultAsync(t => t.MatchId == id);
        if (ts is null) return NotFound();

        var now = DateTime.UtcNow;
        if (ts.IsRunning && ts.QuarterEndsAtUtc is not null)
            ts.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((ts.QuarterEndsAtUtc.Value - now).TotalSeconds));

        ts.IsRunning = false;
        ts.QuarterEndsAtUtc = null;
        ts.LastChangedUtc = now;

        await db.SaveChangesAsync();
        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerPaused", new { remainingSeconds = ts.RemainingSeconds });

        return Ok(new { ts.RemainingSeconds });
    }

    [HttpPost("{id}/timer/resume")]
    public async Task<IActionResult> ResumeTimer(int id)
    {
        var ts = await db.TimerStates.SingleOrDefaultAsync(t => t.MatchId == id);
        if (ts is null) return NotFound();
        if (ts.RemainingSeconds <= 0) return BadRequest("Nothing to resume");

        var now = DateTime.UtcNow;
        ts.IsRunning = true;
        ts.LastChangedUtc = now;
        ts.QuarterEndsAtUtc = now.AddSeconds(ts.RemainingSeconds); // ✅ ahora es DateTime

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("timerResumed", new { quarterEndsAtUtc = ts.QuarterEndsAtUtc, remainingSeconds = ts.RemainingSeconds });

        return Ok(new { remainingSeconds = ts.RemainingSeconds, quarterEndsAtUtc = ts.QuarterEndsAtUtc });
    }


    [HttpPost("{id}/timer/reset")]
    public async Task<IActionResult> ResetTimer(int id)
    {
        var ts = await db.TimerStates.SingleOrDefaultAsync(t => t.MatchId == id);
        if (ts is null) return NotFound();

        ts.IsRunning = false;
        ts.RemainingSeconds = 0;
        ts.QuarterEndsAtUtc = null;
        ts.LastChangedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerReset", new { remainingSeconds = ts.RemainingSeconds });

        return Ok(new { ts.RemainingSeconds });
    }

    // ===========================
    // Avanzar periodo (Quarter)
    // ===========================
    [HttpPost("{id}/quarters/advance")]
    public async Task<IActionResult> AdvanceQuarter(int id)
    {
        var ts = await db.TimerStates.SingleOrDefaultAsync(t => t.MatchId == id);
        if (ts is null) return NotFound();

        if (ts.CurrentQuarter < 4)
            ts.CurrentQuarter += 1;

        // detiene y resetea el temporizador
        ts.IsRunning = false;
        ts.RemainingSeconds = 0;
        ts.QuarterEndsAtUtc = null;
        ts.LastChangedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = ts.CurrentQuarter });

        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-end" });

        return Ok(new { quarter = ts.CurrentQuarter });
    }

    [HttpPost("{id}/quarters/auto-advance")]
    public async Task<IActionResult> AutoAdvanceQuarter(int id)
    {
        var ts = await db.TimerStates.SingleOrDefaultAsync(t => t.MatchId == id);
        if (ts is null) return NotFound();

        var now = DateTime.UtcNow;
        var tolerance = TimeSpan.FromSeconds(1); // ✅ margen
        var remaining = ts.IsRunning && ts.QuarterEndsAtUtc is not null
            ? Math.Max(0, (int)Math.Ceiling((ts.QuarterEndsAtUtc.Value - now - tolerance).TotalSeconds))
            : ts.RemainingSeconds;

        if (remaining > 0)
            return BadRequest("Quarter not finished");

        if (ts.CurrentQuarter < 4) ts.CurrentQuarter += 1;
        else
        {
            var m = await db.Matches.FindAsync(id);
            if (m is not null) m.Status = "Finished";
        }

        ts.IsRunning = false;
        ts.RemainingSeconds = 0;
        ts.QuarterEndsAtUtc = null;
        ts.LastChangedUtc = now;

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = ts.CurrentQuarter });
        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-end" });

        // (opcional) si terminó el 4º, avisa fin de partido:
        if (ts.CurrentQuarter >= 4)
        {
            var m = await db.Matches.FindAsync(id);
            if (m is not null)
            {
                await hub.Clients.Group($"match-{id}")
                    .SendAsync("gameEnded", new
                    {
                        home = m.HomeScore,
                        away = m.AwayScore,
                        winner = m.HomeScore == m.AwayScore ? "draw" : (m.HomeScore > m.AwayScore ? "home" : "away")
                    });
            }
        }

        return Ok(new { quarter = ts.CurrentQuarter });
    }




}
