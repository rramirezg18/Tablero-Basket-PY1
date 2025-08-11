using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Infrastructure;
using Scoreboard.Api.Hubs;
using Scoreboard.Api.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// 1) Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2) EF Core (SQL Server) - usa la cadena de conexión "Default"
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// 3) SignalR (opcional)
builder.Services.AddSignalR();

// 4) CORS solo para desarrollo (Angular dev server en 4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 5) Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors"); // habilita CORS solo en dev
}

// 6) Archivos estáticos (wwwroot) para servir Angular en PROD
// Si copiaste el build de Angular a wwwroot (Dockerfile), esto servirá el front.
app.UseDefaultFiles();  // busca index.html por defecto
app.UseStaticFiles();   // sirve /wwwroot

app.MapControllers();

// 7) SignalR hub (si lo usas)
app.MapHub<ScoreHub>("/hubs/score");

// 8) SPA Fallback: si no hay ruta de API/archivo, devuelve index.html
app.MapFallbackToFile("/index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Teams.Any())
    {
        var home = new Team { Name = "Locales", Color = "#0044FF" };
        var away = new Team { Name = "Visitantes", Color = "#FF3300" };
        db.AddRange(home, away);
        await db.SaveChangesAsync();

        db.Matches.Add(new Match
        {
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            Status = "Live",
            CurrentQuarter = 1,
            QuarterDurationSeconds = 600
        });
        await db.SaveChangesAsync();
    }
}

app.Run();
