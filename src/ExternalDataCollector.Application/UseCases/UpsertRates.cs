using ExternalDataCollector.Application.Abstractions;
using ExternalDataCollector.Domain.Entities;

namespace ExternalDataCollector.Application.UseCases;

public sealed class UpsertRates
{
    private readonly IRateScraper _scraper;
    private readonly IExchangeRateRepository _repo;
    private readonly IClock _clock;

    public UpsertRates(IRateScraper scraper, IExchangeRateRepository repo, IClock clock)
    {
        _scraper = scraper;
        _repo = repo;
        _clock = clock;
    }

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var scraped = await _scraper.ScrapeAsync(ct);

        var now = _clock.UtcNow;
        var entities = scraped.Select(r => new ExchangeRate(
            r.BaseCurrency, r.QuoteCurrency, r.Rate, r.AsOfDate, now));

        await _repo.UpsertManyAsync(entities, ct);
        return scraped.Count;
    }
}
