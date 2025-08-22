using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Infrastructure;

namespace Scoreboard.Api.Controllers;

[ApiController]
[Route("api/posiciones")]
public class PosicionesController(AppDbContext db) : ControllerBase
{
    // GET /api/standings
    [HttpGet]
    public async Task<IActionResult> GetStandings()
    {
        // Cuenta victorias por equipo (VictoriasEquipo)
        var victoriasPorEquipo = db.VictoriasEquipo
            .GroupBy(tw => tw.EquipoId)
            .Select(g => new { EquipoId = g.Key, Victorias = g.Count() });

        // Incluye también equipos con 0 victorias
        var rows = await db.Equipos
            .Select(t => new
            {
                id = t.Id,
                nombre = t.Nombre,
                color = t.Color,
                victorias = victoriasPorEquipo.Where(w => w.EquipoId == t.Id)
                                 .Select(w => w.Victorias)
                                 .FirstOrDefault()
            })
            .OrderByDescending(x => x.victorias)
            .ThenBy(x => x.nombre)
            .ToListAsync();

        return Ok(rows);
    }
}
