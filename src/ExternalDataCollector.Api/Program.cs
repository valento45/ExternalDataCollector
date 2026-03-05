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

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));

builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddScoped<GetLatestRates>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Pega o caminho do banco da Connection String
    var connectionString = builder.Configuration.GetConnectionString("Sqlite");
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;

    // Pega o diretório (ex: "data")
    var directory = Path.GetDirectoryName(dataSource);

    // Se o diretório foi informado e não existe, cria ele
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
    }


    await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/rates/latest", async (string? quote, int? take, GetLatestRates uc, CancellationToken ct) =>
{
    var data = await uc.ExecuteAsync(quote, take ?? 50, ct);
    return Results.Ok(data.Select(x => new
    {
        x.BaseCurrency,
        x.QuoteCurrency,
        x.Rate,
        AsOfDate = x.AsOfDate.ToString("yyyy-MM-dd"),
        x.RetrievedAt
    }));
});

app.MapGet("/api/rates", async (string date, string? quote, int? take, GetLatestRates uc, CancellationToken ct) =>
{
    if (!DateOnly.TryParse(date, out var d))
        return Results.BadRequest(new { error = "Parâmetro 'date' inválido. Use yyyy-MM-dd." });

    var data = await uc.ExecuteByDateAsync(d, quote, take ?? 50, ct);
    return Results.Ok(data.Select(x => new
    {
        x.BaseCurrency,
        x.QuoteCurrency,
        x.Rate,
        AsOfDate = x.AsOfDate.ToString("yyyy-MM-dd"),
        x.RetrievedAt
    }));
});

app.Run();
