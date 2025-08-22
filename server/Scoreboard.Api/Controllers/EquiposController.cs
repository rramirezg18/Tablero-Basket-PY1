using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Infrastructure;
using Scoreboard.Api.Models.Entities; // <= Entidades aquí

namespace Scoreboard.Api.Controllers;

[ApiController]
[Route("api/equipos")]
public class EquiposController(AppDbContext db) : ControllerBase
{
    // GET /api/teams  (lista simple para seleccionar en el front)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q = null)
    {
        var query = db.Equipos.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t => t.Nombre.Contains(term));
        }

        var items = await query
            .OrderBy(t => t.Nombre)
            .Select(t => new
            {
                id = t.Id,
                nombre = t.Nombre,
                color = t.Color,
                cantidadJugadores = db.Jugadores.Count(p => p.EquipoId == t.Id)
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/teams/{id} (detalles + jugadores)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var equipo = await db.Equipos.FirstOrDefaultAsync(t => t.Id == id);
        if (equipo is null) return NotFound();

        var jugadores = await db.Jugadores
            .Where(p => p.EquipoId == id)
            .OrderBy(p => p.Numero)
            .Select(p => new { id = p.Id, numero = p.Numero, nombre = p.Nombre })
            .ToListAsync();

        return Ok(new
        {
            id = equipo.Id,
            nombre = equipo.Nombre,
            color = equipo.Color,
            jugadores
        });
    }

    // DTOs locales para crear equipo con jugadores (pueden quedarse aquí)
    public record JugadorItemDto(int? Numero, string Nombre);
    public record CrearEquipoDto(string Nombre, string? Color, List<JugadorItemDto>? Jugadores);

    // POST /api/teams  (registrar equipo + jugadores)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearEquipoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("El nombre del equipo es obligatorio");

        var nombre = dto.Nombre.Trim();

        var exists = await db.Equipos.AnyAsync(t => t.Nombre == nombre);
        if (exists) return Conflict("El nombre del equipo ya existe");

        var equipo = new Equipo
        {
            Nombre = nombre,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color!.Trim()
        };

        db.Equipos.Add(equipo);
        await db.SaveChangesAsync();

        if (dto.Jugadores is { Count: > 0 })
        {
            // Validar dorsales duplicados dentro del mismo equipo
            var numeros = dto.Jugadores
                .Where(p => p.Numero.HasValue)
                .Select(p => p.Numero!.Value)
                .ToList();
            if (numeros.Count != numeros.Distinct().Count())
                return BadRequest("Números de camiseta duplicados en el mismo equipo");

            foreach (var p in dto.Jugadores)
            {
                var jugador = new Jugador
                {
                    EquipoId = equipo.Id,
                    Numero = p.Numero, // puede ser null
                    Nombre = (p.Nombre ?? string.Empty).Trim()
                };
                db.Jugadores.Add(jugador);
            }
            await db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = equipo.Id }, new { id = equipo.Id, nombre = equipo.Nombre });
    }
}
