using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Hubs;
using Scoreboard.Api.Infrastructure;
using Scoreboard.Api.Models.DTOs;        // DTOs aquí

// 🔽 Alias de Entities para evitar cualquier choque de tipos
using PartidoEntity       = Scoreboard.Api.Models.Entities.Partido;
using EquipoEntity        = Scoreboard.Api.Models.Entities.Equipo;
using EventoPuntajeEntity = Scoreboard.Api.Models.Entities.EventoPuntaje;
using FaltaEntity         = Scoreboard.Api.Models.Entities.Falta;
using VictoriaEquipoEntity= Scoreboard.Api.Models.Entities.VictoriaEquipo;

namespace Scoreboard.Api.Controllers;

[ApiController]
[Route("api/partidos")]
public class PartidosController(AppDbContext db, IHubContext<ScoreHub> hub, IMatchRunTimeStore rt) : ControllerBase
{
    // ===========================
    // GET estado del partido
    // ===========================
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var m = await db.Partidos
            .Include(x => x.EquipoLocal)
            .Include(x => x.EquipoVisitante)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (m is null) return NotFound();

        // Snapshot del runtime (no BD)
        var snap = rt.GetOrCreate(id, m.DuracionPeriodoSegundos);

        // Conteo de faltas por equipo
        var faltasLocal = await db.Faltas.CountAsync(f => f.PartidoId == id && f.EquipoId == m.EquipoLocalId);
        var faltasVisitante = await db.Faltas.CountAsync(f => f.PartidoId == id && f.EquipoId == m.EquipoVisitanteId);

        return Ok(new
        {
            id = m.Id,
            equipoLocalId = m.EquipoLocalId,
            equipoVisitanteId = m.EquipoVisitanteId,
            equipoLocal = m.EquipoLocal.Nombre,
            equipoVisitante = m.EquipoVisitante.Nombre,
            puntajeLocal = m.PuntajeLocal,
            puntajeVisitante = m.PuntajeVisitante,
            estado = m.Estado,
            duracionPeriodoSegundos = m.DuracionPeriodoSegundos,
            periodo = m.Periodo, // mantiene el nombre 'quarter' para el front
            timer = new
            {
                corriendo = snap.IsRunning,
                segundosRestantes = snap.RemainingSeconds,
                periodoTerminaUtc = snap.EndsAt
            },
            faltasLocal,
            faltasVisitante
        });
    }

    // ===========================
    // NEW GAME (nombres libres)
    // ===========================
    [HttpPost("new")]
    public async Task<IActionResult> NewGame([FromBody] NuevoPartidoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreLocal) || string.IsNullOrWhiteSpace(dto.NombreVisitante))
            return BadRequest("Se requieren nombres de equipos");

        var home = new EquipoEntity { Name = dto.NombreLocal.Trim() };
        var away = new EquipoEntity { Name = dto.NombreVisitante.Trim() };
        db.AddRange(home, away);
        await db.SaveChangesAsync();

        var match = new PartidoEntity
        {
            EquipoLocalId = home.Id,
            EquipoVisitanteId = away.Id,
            Estado = "Scheduled",
            DuracionPeriodoSegundos = dto.DuracionPeriodoSegundos is > 0 ? dto.DuracionPeriodoSegundos!.Value : 600,
            PuntajeLocal = 0,
            PuntajeVisitante = 0,
            Periodo = 1,
            FechaPartido = DateTime.Now
        };
        db.Partidos.Add(match);
        await db.SaveChangesAsync();

        // Limpia cualquier estado previo en runtime
        rt.Reset(match.Id);

        return Ok(new
        {
            partidoId = match.Id,
            equipoLocalId = home.Id,
            equipoVisitanteId = away.Id,
            duracionPeriodoSegundos = match.DuracionPeriodoSegundos
        });
    }

    // ===========================
    // NEW GAME BY TEAM IDs
    // ===========================
    [HttpPost("new-by-teams")]
    public async Task<IActionResult> NuevoPorEquipos([FromBody] NuevoPartidoPorEquiposDto dto)
    {
        if (dto.EquipoLocalId <= 0 || dto.EquipoVisitanteId <= 0 || dto.EquipoLocalId == dto.EquipoVisitanteId)
            return BadRequest("Selecciona dos equipos diferentes");

        var home = await db.Equipos.FindAsync(dto.EquipoLocalId);
        var away = await db.Equipos.FindAsync(dto.EquipoVisitanteId);
        if (home is null || away is null) return BadRequest("Ids de equipo inválidos");

        var match = new PartidoEntity
        {
            EquipoLocalId = home.Id,
            EquipoVisitanteId = away.Id,
            Estado = "Scheduled",
            DuracionPeriodoSegundos = dto.DuracionPeriodoSegundos is > 0 ? dto.DuracionPeriodoSegundos!.Value : 600,
            PuntajeLocal = 0,
            PuntajeVisitante = 0,
            Periodo = 1,
            FechaPartido = DateTime.Now
        };

        db.Partidos.Add(match);
        await db.SaveChangesAsync();

        rt.Reset(match.Id);

        return Ok(new
        {
            partidoId = match.Id,
            equipoLocalId = home.Id,
            equipoVisitanteId = away.Id,
            duracionPeriodoSegundos = match.DuracionPeriodoSegundos
        });
    }

    // ===========================
    // SCORE (+1/+2/+3)
    // ===========================
    [HttpPost("{id}/score")]
    public async Task<IActionResult> AddScore(int id, [FromBody] AgregarPuntajeDto dto)
    {
        if (dto.Puntos is not (1 or 2 or 3)) return BadRequest("Los puntos deben ser 1,2,3");

        var m = await db.Partidos.FindAsync(id);
        if (m is null || m.Estado != "Live") return NotFound();

        if (dto.EquipoId == m.EquipoLocalId) m.PuntajeLocal += dto.Puntos;
        else if (dto.EquipoId == m.EquipoVisitanteId) m.PuntajeVisitante += dto.Puntos;
        else return BadRequest("Equipo inválido para este partido");

        db.EventosPuntaje.Add(new EventoPuntajeEntity { PartidoId = id, EquipoId = dto.EquipoId, Puntos = dto.Puntos, FechaRegistro = DateTime.Now });
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("puntajeActualizado", new { puntajeLocal = m.PuntajeLocal, puntajeVisitante = m.PuntajeVisitante });

        return Ok();
    }

    // ===========================
    // SCORE (ajuste -1, etc.)
    // ===========================
    [HttpPost("{id}/score/adjust")]
    public async Task<IActionResult> AdjustScore(int id, [FromBody] AjustarPuntajeDto dto)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        if (dto.EquipoId == m.EquipoLocalId)
        {
            if (m.PuntajeLocal + dto.Delta < 0)
                return BadRequest("El puntaje no puede ser negativo");
            m.PuntajeLocal += dto.Delta;
        }
        else if (dto.EquipoId == m.EquipoVisitanteId)
        {
            if (m.PuntajeVisitante + dto.Delta < 0)
                return BadRequest("El puntaje no puede ser negativo");
            m.PuntajeVisitante += dto.Delta;
        }
        else return BadRequest("Equipo inválido para este partido");

        db.EventosPuntaje.Add(new EventoPuntajeEntity { PartidoId = id, EquipoId = dto.EquipoId, Puntos = dto.Delta, FechaRegistro = DateTime.Now });
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("puntajeActualizado", new { puntajeLocal = m.PuntajeLocal, puntajeVisitante = m.PuntajeVisitante });

        return Ok();
    }

    // ===========================
    // FOULS (sumar/restar)
    // ===========================
    [HttpPost("{id}/fouls")]
    public async Task<IActionResult> AgregarFalta(int id, [FromBody] AgregarFaltaDto dto)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        if (dto.EquipoId != m.EquipoLocalId && dto.EquipoId != m.EquipoVisitanteId)
            return BadRequest("Equipo inválido para este partido");

        db.Faltas.Add(new FaltaEntity
        {
            PartidoId = id,
            EquipoId = dto.EquipoId,
            JugadorId = dto.JugadorId,
            Tipo = dto.Tipo,
            FechaRegistro = DateTime.Now
        });
        await db.SaveChangesAsync();

        var faltasLocal = await db.Faltas.CountAsync(f => f.PartidoId == id && f.EquipoId == m.EquipoLocalId);
        var faltasVisitante = await db.Faltas.CountAsync(f => f.PartidoId == id && f.EquipoId == m.EquipoVisitanteId);

        await hub.Clients.Group($"match-{id}")
            .SendAsync("faltasActualizadas", new { faltasLocal, faltasVisitante });

        return Ok(new { faltasLocal, faltasVisitante });
    }

    [HttpPost("{id}/fouls/adjust")]
    public async Task<IActionResult> AjustarFalta(int id, [FromBody] AjustarFaltaDto dto)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        if (dto.EquipoId != m.EquipoLocalId && dto.EquipoId != m.EquipoVisitanteId)
            return BadRequest("Equipo inválido para este partido");

        if (dto.Delta > 0)
        {
            for (int i = 0; i < dto.Delta; i++)
            {
                db.Faltas.Add(new FaltaEntity { PartidoId = id, EquipoId = dto.EquipoId, FechaRegistro = DateTime.Now });
            }
        }
        else if (dto.Delta < 0)
        {
            var toRemove = await db.Faltas
                .Where(f => f.PartidoId == id && f.EquipoId == dto.EquipoId)
                .OrderByDescending(f => f.Id)
                .Take(Math.Abs(dto.Delta))
                .ToListAsync();

            if (toRemove.Count == 0)
                return BadRequest("No hay faltas para eliminar");

            db.Faltas.RemoveRange(toRemove);
        }

        await db.SaveChangesAsync();

        var faltasLocal = await db.Faltas.CountAsync(f => f.PartidoId == id && f.EquipoId == m.EquipoLocalId);
        var faltasVisitante = await db.Faltas.CountAsync(f => f.PartidoId == id && f.EquipoId == m.EquipoVisitanteId);

        await hub.Clients.Group($"match-{id}")
            .SendAsync("faltasActualizadas", new { faltasLocal, faltasVisitante });

        return Ok(new { faltasLocal, faltasVisitante });
    }

    // ===========================
    // TIMER (runtime, no BD)
    // ===========================
    public record StartTimerDto(int? DuracionPeriodoSegundos); // este DTO puede vivir aquí

    [HttpPost("{id}/timer/start")]
    public async Task<IActionResult> StartTimer(int id, [FromBody] StartTimerDto? dto)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        if (dto?.DuracionPeriodoSegundos is int q && q > 0)
            m.DuracionPeriodoSegundos = q;

        m.Estado = "Live";
        await db.SaveChangesAsync();

        rt.Start(id, m.DuracionPeriodoSegundos);
        var snap = rt.Get(id);

        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerStarted", new { quarterEndsAtUtc = snap.EndsAt, remainingSeconds = snap.RemainingSeconds });

        await hub.Clients.Group($"match-{id}")
          .SendAsync("quarterChanged", new { quarter = m.Periodo });
        await hub.Clients.Group($"match-{id}")
          .SendAsync("buzzer", new { reason = "quarter-start" });

        return Ok(new { remainingSeconds = snap.RemainingSeconds, quarterEndsAtUtc = snap.EndsAt });
    }

    [HttpPost("{id}/timer/pause")]
    public async Task<IActionResult> PauseTimer(int id)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        var rem = rt.Pause(id);

        await hub.Clients.Group($"match-{id}")
          .SendAsync("timerPaused", new { remainingSeconds = rem });

        return Ok(new { remainingSeconds = rem });
    }

    [HttpPost("{id}/timer/resume")]
    public async Task<IActionResult> ResumeTimer(int id)
    {
        var m = await db.Partidos.FindAsync(id);
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
        var m = await db.Partidos.FindAsync(id);
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
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        if (m.Periodo < 4)
        {
            m.Periodo += 1;
        }
        else
        {
            // Termina el partido
            m.Estado = "Finished";
            await RecordWinIfFinishedAsync(m);
        }

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = m.Periodo });
        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-end" });

        return Ok(new { quarter = m.Periodo });
    }

    [HttpPost("{id}/quarters/auto-advance")]
    public async Task<IActionResult> AutoAdvanceQuarter(int id)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        if (m.Estado == "Finished")
            return Ok(new { quarter = m.Periodo }); // Ya estaba terminado

        if (m.Periodo < 4)
        {
            m.Periodo += 1;
        }
        else
        {
            m.Estado = "Finished";
            await RecordWinIfFinishedAsync(m);
        }

        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("quarterChanged", new { quarter = m.Periodo });
        await hub.Clients.Group($"match-{id}")
            .SendAsync("buzzer", new { reason = "quarter-end" });

        if (m.Estado == "Finished")
        {
            await hub.Clients.Group($"match-{id}")
                .SendAsync("gameEnded", new
                {
                    home = m.PuntajeLocal,
                    away = m.PuntajeVisitante,
                    winner = m.PuntajeLocal == m.PuntajeVisitante ? "draw" : (m.PuntajeLocal > m.PuntajeVisitante ? "home" : "away")
                });
        }

        return Ok(new { quarter = m.Periodo });
    }

    // ===========================
    // CANCELAR O SUSPENDER PARTIDO
    // ===========================
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        m.Estado = "Canceled";
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("gameCanceled", new { status = m.Estado });

        return Ok(new { status = m.Estado });
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> Suspend(int id)
    {
        var m = await db.Partidos.FindAsync(id);
        if (m is null) return NotFound();

        m.Estado = "Suspended";
        await db.SaveChangesAsync();

        await hub.Clients.Group($"match-{id}")
            .SendAsync("gameSuspended", new { status = m.Estado });

        return Ok(new { status = m.Estado });
    }

    // ===========================
    // PRIVADO: registrar VictoriaEquipo si el partido terminó y no es empate
    // ===========================
    private async Task RecordWinIfFinishedAsync(PartidoEntity m)
    {
        if (m.Estado != "Finished") return;

        // Empate: no se registra victoria
        if (m.PuntajeLocal == m.PuntajeVisitante) return;

        var equipoGanadorId = m.PuntajeLocal > m.PuntajeVisitante ? m.EquipoLocalId : m.EquipoVisitanteId;

        // Evitar duplicado (por reintentos)
        var exists = await db.VictoriasEquipo.AnyAsync(tw => tw.EquipoId == equipoGanadorId && tw.PartidoId == m.Id);
        if (!exists)
        {
            db.VictoriasEquipo.Add(new VictoriaEquipoEntity
            {
                EquipoId = equipoGanadorId,
                PartidoId = m.Id,
                FechaRegistro = DateTime.UtcNow
            });
            // Persistirá con el SaveChanges del flujo superior.
        }
    }
}