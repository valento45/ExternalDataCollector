using ExternalDataCollector.Application.Abstractions;
using ExternalDataCollector.Domain.Entities;

namespace ExternalDataCollector.Application.UseCases;

public sealed class GetLatestRates
{
    private readonly IExchangeRateRepository _repo;

    public GetLatestRates(IExchangeRateRepository repo) => _repo = repo;

    public Task<IReadOnlyList<ExchangeRate>> ExecuteAsync(string? quoteCurrency, int take, CancellationToken ct)
        => _repo.GetLatestAsync(quoteCurrency, take, ct);

    public Task<IReadOnlyList<ExchangeRate>> ExecuteByDateAsync(DateOnly date, string? quoteCurrency, int take, CancellationToken ct)
        => _repo.GetByDateAsync(date, quoteCurrency, take, ct);
}
