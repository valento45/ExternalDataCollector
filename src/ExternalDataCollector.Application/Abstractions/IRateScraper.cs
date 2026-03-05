namespace ExternalDataCollector.Application.Abstractions;

public sealed record ScrapedRate(string BaseCurrency, string QuoteCurrency, decimal Rate, DateOnly AsOfDate);

public interface IRateScraper
{
    Task<IReadOnlyList<ScrapedRate>> ScrapeAsync(CancellationToken ct);
}
