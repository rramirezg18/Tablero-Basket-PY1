using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Infrastructure;
using Scoreboard.Api.Hubs;

// ⬇️ Alias explícitos a Models.Entities
using EquipoEntity  = Scoreboard.Api.Models.Entities.Equipo;
using PartidoEntity = Scoreboard.Api.Models.Entities.Partido;

var builder = WebApplication.CreateBuilder(args);

// 1) Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2) EF Core (SQL Server)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3) SignalR
builder.Services.AddSignalR();

// 4) CORS (Angular dev server)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 5) Runtime del reloj en memoria
builder.Services.AddSingleton<IMatchRunTimeStore, MatchRunTimeStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<ScoreHub>("/hubs/score");
app.MapFallbackToFile("/index.html");

// ===== Migraciones + seed mínimo =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Equipos.Any())
    {
        var home = new EquipoEntity { Nombre = "Locales", Color = "#0044FF" };
        var away = new EquipoEntity { Nombre = "Visitantes", Color = "#FF3300" };
        db.AddRange(home, away);
        await db.SaveChangesAsync();

        // 👇 Usamos Set<PartidoEntity>() para evitar cualquier choque de tipos
        db.Set<PartidoEntity>().Add(new PartidoEntity
        {
            EquipoLocalId = home.Id,
            EquipoVisitanteId = away.Id,
            Estado = "Programado",
            DuracionPeriodoSegundos = 600,
            PuntajeLocal = 0,
            PuntajeVisitante = 0,
            Periodo = 1,
            FechaPartido = DateTime.Now
        });
        await db.SaveChangesAsync();
    }
}

app.Run();
