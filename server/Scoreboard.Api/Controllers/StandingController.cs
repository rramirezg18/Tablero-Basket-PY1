
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Infrastructure;

namespace Scoreboard.Api.Controllers;

[ApiController]
[Route("api/standings")]
public class StandingsController(AppDbContext db) : ControllerBase
{
    // GET /api/standings
    [HttpGet]
    public async Task<IActionResult> GetStandings()
    {
        // Cuenta victorias por equipo (TeamWins)
        var winsByTeam = db.TeamWins
            .GroupBy(tw => tw.TeamId)
            .Select(g => new { TeamId = g.Key, Wins = g.Count() });

        // Incluye también equipos con 0 victorias
        var rows = await db.Teams
            .Select(t => new
            {
                id = t.Id,
                name = t.Name,
                color = t.Color,
                wins = winsByTeam.Where(w => w.TeamId == t.Id)
                                 .Select(w => w.Wins)
                                 .FirstOrDefault()
            })
            .OrderByDescending(x => x.wins)
            .ThenBy(x => x.name)
            .ToListAsync();

        return Ok(rows);
    }
}
