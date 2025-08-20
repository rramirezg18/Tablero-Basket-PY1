using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Hubs;
using Scoreboard.Api.Infrastructure;
using Scoreboard.Api.Models.DTOs;        // DTOs aquí

// 🔽 Alias de Entities para evitar cualquier choque de tipos
using MatchEntity     = Scoreboard.Api.Models.Entities.Match;
using TeamEntity      = Scoreboard.Api.Models.Entities.Team;
using ScoreEventEntity= Scoreboard.Api.Models.Entities.ScoreEvent;
using FoulEntity      = Scoreboard.Api.Models.Entities.Foul;
using TeamWinEntity   = Scoreboard.Api.Models.Entities.TeamWin;

namespace Scoreboard.Api.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController(AppDbContext db, IHubContext<ScoreHub> hub, IMatchRunTimeStore rt) : ControllerBase
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

        // Snapshot del runtime (no BD)
        var snap = rt.GetOrCreate(id, m.QuarterDurationSeconds);

        // Conteo de faltas por equipo 
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
            quarter = m.Period, // mantiene el nombre 'quarter' para el front
            timer = new
            {
                running = snap.IsRunning,
                remainingSeconds = snap.RemainingSeconds,
                quarterEndsAtUtc = snap.EndsAt
            },
            homeFouls,
            awayFouls
        });
    }

    // ===========================
    // NEW GAME (nombres libres)
    // ===========================
    [HttpPost("new")]
    public async Task<IActionResult> NewGame([FromBody] NewGameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HomeName) || string.IsNullOrWhiteSpace(dto.AwayName))
            return BadRequest("Team names required");

        var home = new TeamEntity { Name = dto.HomeName.Trim() };
        var away = new TeamEntity { Name = dto.AwayName.Trim() };
        db.AddRange(home, away);
        await db.SaveChangesAsync();

        var match = new MatchEntity
        {
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            Status = "Scheduled",
            QuarterDurationSeconds = dto.QuarterDurationSeconds is > 0 ? dto.QuarterDurationSeconds!.Value : 600,
            HomeScore = 0,
            AwayScore = 0,
            Period = 1,
            DateMatch = DateTime.Now
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        // Limpia cualquier estado previo en runtime
        rt.Reset(match.Id);

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
    [HttpPost("new-by-teams")]
    public async Task<IActionResult> NewByTeams([FromBody] NewGameByTeamsDto dto)
    {
        if (dto.HomeTeamId <= 0 || dto.AwayTeamId <= 0 || dto.HomeTeamId == dto.AwayTeamId)
            return BadRequest("Select two different teams");

        var home = await db.Teams.FindAsync(dto.HomeTeamId);
        var away = await db.Teams.FindAsync(dto.AwayTeamId);
        if (home is null || away is null) return BadRequest("Invalid team ids");

        var match = new MatchEntity
        {
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            Status = "Scheduled",
            QuarterDurationSeconds = dto.QuarterDurationSeconds is > 0 ? dto.QuarterDurationSeconds!.Value : 600,
            HomeScore = 0,
            AwayScore = 0,
            Period = 1,
            DateMatch = DateTime.Now
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        rt.Reset(match.Id);

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
    [HttpPost("{id}/score")]
    public async Task<IActionResult> AddScore(int id, [FromBody] AddScoreDto dto)
    {
        if (dto.Points is not (1 or 2 or 3)) return BadRequest("Points must be 1,2,3");

        var m = await db.Matches.FindAsync(id);
        if (m is null || m.Status != "Live") return NotFound();

        if (dto.TeamId == m.HomeTeamId) m.HomeScore += dto.Points;
        else if (dto.TeamId == m.AwayTeamId) m.AwayScore += dto.Points;
        else return BadRequest("Invalid teamId for this match");

        db.ScoreEvents.Add(new ScoreEventEntity { MatchId = id, TeamId = dto.TeamId, Points = dto.Points, DateRegister = DateTime.Now });
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("scoreUpdated", new { homeScore = m.HomeScore, awayScore = m.AwayScore });

        return Ok();
    }

    // ===========================
    // SCORE (ajuste -1, etc.)
    // ===========================
    [HttpPost("{id}/score/adjust")]
    public async Task<IActionResult> AdjustScore(int id, [FromBody] AdjustScoreDto dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (dto.TeamId == m.HomeTeamId) m.HomeScore += dto.Delta;
        else if (dto.TeamId == m.AwayTeamId) m.AwayScore += dto.Delta;
        else return BadRequest("Invalid teamId for this match");

        db.ScoreEvents.Add(new ScoreEventEntity { MatchId = id, TeamId = dto.TeamId, Points = dto.Delta, DateRegister = DateTime.Now });
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("scoreUpdated", new { homeScore = m.HomeScore, awayScore = m.AwayScore });

        return Ok();
    }

    // ===========================
    // FOULS (sumar/restar)
    // ===========================
    [HttpPost("{id}/fouls")]
    public async Task<IActionResult> AddFoul(int id, [FromBody] AddFoulDto dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (dto.TeamId != m.HomeTeamId && dto.TeamId != m.AwayTeamId)
            return BadRequest("Invalid teamId for this match");

        db.Fouls.Add(new FoulEntity
        {
            MatchId = id,
            TeamId = dto.TeamId,
            PlayerId = dto.PlayerId,
            Type = dto.Type,
            DateRegister = DateTime.Now
        });
        await db.SaveChangesAsync();

        var homeFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.HomeTeamId);
        var awayFouls = await db.Fouls.CountAsync(f => f.MatchId == id && f.TeamId == m.AwayTeamId);

        await hub.Clients.Group($"match-{id}")
            .SendAsync("foulsUpdated", new { homeFouls, awayFouls });

        return Ok(new { homeFouls, awayFouls });
    }

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
                db.Fouls.Add(new FoulEntity { MatchId = id, TeamId = dto.TeamId, DateRegister = DateTime.Now });
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
    // TIMER (runtime, no BD)
    // ===========================
    public record StartTimerDto(int? QuarterDurationSeconds); // este DTO puede vivir aquí

    [HttpPost("{id}/timer/start")]
    public async Task<IActionResult> StartTimer(int id, [FromBody] StartTimerDto? dto)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (dto?.QuarterDurationSeconds is int q && q > 0)
            m.QuarterDurationSeconds = q;

        m.Status = "Live";
        await db.SaveChangesAsync();

        rt.Start(id, m.QuarterDurationSeconds);
        var snap = rt.Get(id);

        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerStarted", new { quarterEndsAtUtc = snap.EndsAt, remainingSeconds = snap.RemainingSeconds });

        await hub.Clients.Group($"match-{id}")
          .SendAsync("quarterChanged", new { quarter = m.Period });
        await hub.Clients.Group($"match-{id}")
          .SendAsync("buzzer", new { reason = "quarter-start" });

        return Ok(new { remainingSeconds = snap.RemainingSeconds, quarterEndsAtUtc = snap.EndsAt });
    }

    [HttpPost("{id}/timer/pause")]
    public async Task<IActionResult> PauseTimer(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        var rem = rt.Pause(id);

        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerPaused", new { remainingSeconds = rem });

        return Ok(new { remainingSeconds = rem });
    }

    [HttpPost("{id}/timer/resume")]
    public async Task<IActionResult> ResumeTimer(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        var snapBefore = rt.Get(id);
        if (snapBefore.RemainingSeconds <= 0) return BadRequest("Nothing to resume");

        rt.Resume(id);
        var snap = rt.Get(id);

        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerResumed", new { quarterEndsAtUtc = snap.EndsAt, remainingSeconds = snap.RemainingSeconds });

        return Ok(new { remainingSeconds = snap.RemainingSeconds, quarterEndsAtUtc = snap.EndsAt });
    }

    [HttpPost("{id}/timer/reset")]
    public async Task<IActionResult> ResetTimer(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        rt.Reset(id);

        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerReset", new { remainingSeconds = 0 });

        return Ok(new { remainingSeconds = 0 });
    }

    // ===========================
    // Avanzar periodo (Quarter)
    // ===========================
    [HttpPost("{id}/quarters/advance")]
    public async Task<IActionResult> AdvanceQuarter(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (m.Period < 4)
        {
            m.Period += 1;
        }
        else
        {
            // Termina el partido
            m.Status = "Finished";
            await RecordWinIfFinishedAsync(m);
        }

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = m.Period });
        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-end" });

        return Ok(new { quarter = m.Period });
    }

    [HttpPost("{id}/quarters/auto-advance")]
    public async Task<IActionResult> AutoAdvanceQuarter(int id)
    {
        var m = await db.Matches.FindAsync(id);
        if (m is null) return NotFound();

        if (m.Status == "Finished")
            return Ok(new { quarter = m.Period }); // Ya estaba terminado

        if (m.Period < 4)
        {
            m.Period += 1;
        }
        else
        {
            m.Status = "Finished";
            await RecordWinIfFinishedAsync(m);
        }

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = m.Period });
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

        return Ok(new { quarter = m.Period });
    }

    // ===========================
    // PRIVADO: registrar TeamWin si el partido terminó y no es empate
    // ===========================
    private async Task RecordWinIfFinishedAsync(MatchEntity m)
    {
        if (m.Status != "Finished") return;

        // Empate: no se registra victoria
        if (m.HomeScore == m.AwayScore) return;

        var winnerTeamId = m.HomeScore > m.AwayScore ? m.HomeTeamId : m.AwayTeamId;

        // Evitar duplicado (por reintentos)
        var exists = await db.TeamWins.AnyAsync(tw => tw.TeamId == winnerTeamId && tw.MatchId == m.Id);
        if (!exists)
        {
            db.TeamWins.Add(new TeamWinEntity
            {
                TeamId = winnerTeamId,
                MatchId = m.Id,
                DateRegistered = DateTime.UtcNow
            });
            // Persistirá con el SaveChanges del flujo superior.
        }
    }
}
