using Microsoft.EntityFrameworkCore;
using Scoreboard.Api.Infrastructure;
using Scoreboard.Api.Hubs;
using Scoreboard.Api.Domain.Entities;

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

var app = builder.Build();

// 5) Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}

// 6) Archivos estáticos (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// 7) SignalR hub
app.MapHub<ScoreHub>("/hubs/score");

// 8) SPA Fallback
app.MapFallbackToFile("/index.html");

// Migraciones + seed mínimo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Teams.Any())
    {
        var home = new Team { Name = "Locales", Color = "#0044FF" };
        var away  = new Team { Name = "Visitantes", Color = "#FF3300" };
        db.AddRange(home, away);
        await db.SaveChangesAsync();

        db.Matches.Add(new Match
        {
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            Status = "Scheduled",
            QuarterDurationSeconds = 600,
            HomeScore = 0,
            AwayScore = 0,

            // Estado de período inicial
            Period = 1,
            IsRunning = false,
            RemainingSeconds = 0,
            PeriodEnd = null,

            StartMatch = null,
            DateMatch = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}

app.Run();
