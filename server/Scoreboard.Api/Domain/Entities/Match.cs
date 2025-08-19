using System;
using System.Collections.Generic;

namespace Scoreboard.Api.Domain.Entities;

public class Match
{
    public int Id { get; set; }

    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    public string Status { get; set; } = "Scheduled";

    // Duración del período en segundos (configurable por partido)
    public int QuarterDurationSeconds { get; set; } = 600;

    public int HomeScore { get; set; }
    public int AwayScore { get; set; }

    // ===== Estado del reloj/período ahora en Match =====
    public int Period { get; set; } = 1;           // 1..4 (antes CurrentQuarter)
    public bool IsRunning { get; set; } = false;
    public int RemainingSeconds { get; set; } = 0;
    public DateTime? PeriodEnd { get; set; }       // antes QuarterEndsAtUtc

    // Metadatos existentes (renombrados)
    public DateTime? StartMatch { get; set; }      // antes StartTimeUtc
    public DateTime DateMatch { get; set; } = DateTime.UtcNow; // antes CreatedUtc

    // Navegación
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    public ICollection<ScoreEvent> ScoreEvents { get; set; } = new List<ScoreEvent>();
}
