using ExternalDataCollector.Application.Abstractions;
using ExternalDataCollector.Application.UseCases;
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

var cs = builder.Configuration.GetConnectionString("Sqlite")
         ?? "Data Source=/app/data/rates.db";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));
builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
builder.Services.AddSingleton<IClock, SystemClock>();

// HttpClient + Resiliência (retry com backoff + jitter, trata 5xx/408/429 e falhas de rede)
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

// Scraper recebe moedas desejadas
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

await host.RunAsync();
