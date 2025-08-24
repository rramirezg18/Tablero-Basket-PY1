using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Infrastructure;
using Scoreboard.Models.Entities; 
namespace Scoreboard.Controllers;

[ApiController]
[Route("api/teams")]
public class TeamsController(AppDbContext db) : ControllerBase
{
    // GET /api/teams  (lista simple para seleccionar en el front)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q = null)
    {
        var query = db.Teams.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t => t.Name.Contains(term));
        }

        var items = await query
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                id = t.Id,
                name = t.Name,
                color = t.Color,
                playersCount = db.Players.Count(p => p.TeamId == t.Id)
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/teams/{id} (detalles + jugadores)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == id);
        if (team is null) return NotFound();

        var players = await db.Players
            .Where(p => p.TeamId == id)
            .OrderBy(p => p.Number)
            .Select(p => new { id = p.Id, number = p.Number, name = p.Name })
            .ToListAsync();

        return Ok(new
        {
            id = team.Id,
            name = team.Name,
            color = team.Color,
            players
        });
    }

    // DTOs locales para crear equipo con jugadores 
    public record PlayerItemDto(int? Number, string Name);
    public record CreateTeamDto(string Name, string? Color, List<PlayerItemDto>? Players);

    // POST /api/teams  (registrar equipo + jugadores)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Team name is required");

        var name = dto.Name.Trim();

        var exists = await db.Teams.AnyAsync(t => t.Name == name);
        if (exists) return Conflict("Team name already exists");

        var team = new Team
        {
            Name = name,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color!.Trim()
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        if (dto.Players is { Count: > 0 })
        {
            // Validar dorsales duplicados dentro del mismo equipo
            var numbers = dto.Players
                .Where(p => p.Number.HasValue)
                .Select(p => p.Number!.Value)
                .ToList();
            if (numbers.Count != numbers.Distinct().Count())
                return BadRequest("Duplicated jersey numbers within the same team");

            foreach (var p in dto.Players)
            {
                var player = new Player
                {
                    TeamId = team.Id,
                    Number = p.Number, // puede ser null
                    Name = (p.Name ?? string.Empty).Trim()
                };
                db.Players.Add(player);
            }
            await db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = team.Id }, new { id = team.Id, name = team.Name });
    }
}
