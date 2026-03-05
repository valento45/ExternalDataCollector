using ExternalDataCollector.Application.Abstractions;
using ExternalDataCollector.Application.UseCases;
using ExternalDataCollector.Infrastructure;
using ExternalDataCollector.Infrastructure.Data;
using ExternalDataCollector.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cs = builder.Configuration.GetConnectionString("Sqlite");


if (string.IsNullOrWhiteSpace(cs))
{
    var dbPath = SqlitePath.GetDefaultDbPath();
    cs = $"Data Source={dbPath}";
}

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));

builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddScoped<GetLatestRates>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var connectionString = builder.Configuration.GetConnectionString("Sqlite");
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;

    var directory = Path.GetDirectoryName(dataSource);

    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
    }


    await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapGet("/healthcheck", () => Results.Ok(new { status = "ok" }));

app.Run();
