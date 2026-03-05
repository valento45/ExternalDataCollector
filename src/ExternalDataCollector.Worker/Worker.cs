using ExternalDataCollector.Application.UseCases;
using Microsoft.Extensions.Hosting;

namespace ExternalDataCollector.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;

    public Worker(IServiceProvider sp, ILogger<Worker> logger, IConfiguration config)
    {
        _sp = sp;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _config.GetValue<int?>("Scraping:IntervalSeconds") ?? 300;
        if (intervalSeconds < 30) intervalSeconds = 30;

        _logger.LogInformation("Worker iniciado. Intervalo: {IntervalSeconds}s", intervalSeconds);

        await RunOnce(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnce(stoppingToken);
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<UpsertRates>();

            var count = await uc.ExecuteAsync(ct);
            _logger.LogInformation("Coleta OK. Itens atualizados/inseridos: {Count}", count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Worker cancelado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na coleta (será tentado novamente no próximo ciclo).");
        }
    }
}
