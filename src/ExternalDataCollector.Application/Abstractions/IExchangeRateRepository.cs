using ExternalDataCollector.Domain.Entities;

namespace ExternalDataCollector.Application.Abstractions;

public interface IExchangeRateRepository
{
    Task UpsertManyAsync(IEnumerable<ExchangeRate> rates, CancellationToken ct);

    Task<IReadOnlyList<ExchangeRate>> GetLatestAsync(string? quoteCurrency, int take, CancellationToken ct);

    Task<IReadOnlyList<ExchangeRate>> GetByDateAsync(DateOnly asOfDate, string? quoteCurrency, int take, CancellationToken ct);
}
