using ExternalDataCollector.Application.Abstractions;
using ExternalDataCollector.Application.UseCases;
using ExternalDataCollector.Infrastructure;
using ExternalDataCollector.Infrastructure.Data;
using ExternalDataCollector.Infrastructure.Scraping;
using ExternalDataCollector.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Http.Headers;

var builder = Host.CreateApplicationBuilder(args);

var cs = builder.Configuration.GetConnectionString("Sqlite");


if (string.IsNullOrWhiteSpace(cs))
{
    var dbPath = SqlitePath.GetDefaultDbPath();
    cs = $"Data Source={dbPath}";
}


builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));
builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();

static IAsyncPolicy<HttpResponseMessage> CreatePolicy()
{
    var jitter = new Random();

    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx + network
        .OrResult(msg => msg.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: attempt =>
            {
                var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2,4,8,16,32
                var extra = TimeSpan.FromMilliseconds(jitter.Next(0, 300));
                return baseDelay + extra;
            });
}

builder.Services.AddHttpClient("scraper", http =>
{
    http.Timeout = TimeSpan.FromSeconds(15);
    http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ExternalDataCollector", "1.0"));
});
//.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
//{
//    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
//    AutomaticDecompression = DecompressionMethods.All
//})
//.AddPolicyHandler(CreatePolicy());

builder.Services.AddTransient<IRateScraper>(sp =>
{
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var http = httpFactory.CreateClient("scraper");

    var wanted = sp.GetRequiredService<IConfiguration>()
        .GetSection("Scraping:WantedQuotes")
        .Get<string[]>() ?? Array.Empty<string>();

    return new EcbXmlRateScraper(http, wanted);
});

builder.Services.AddScoped<UpsertRates>();

builder.Services.AddHostedService<ExternalDataCollector.Worker.Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
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

await host.RunAsync();
