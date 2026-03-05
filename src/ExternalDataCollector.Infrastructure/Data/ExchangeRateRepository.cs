using ExternalDataCollector.Application.Abstractions;
using ExternalDataCollector.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExternalDataCollector.Infrastructure.Data;

public sealed class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly AppDbContext _db;

    public ExchangeRateRepository(AppDbContext db) => _db = db;

    public async Task UpsertManyAsync(IEnumerable<ExchangeRate> rates, CancellationToken ct)
    {
        foreach (var r in rates)
        {
            var existing = await _db.ExchangeRates
                .FirstOrDefaultAsync(x =>
                    x.BaseCurrency == r.BaseCurrency &&
                    x.QuoteCurrency == r.QuoteCurrency &&
                    x.AsOfDate == r.AsOfDate, ct);

            if (existing is null)
            {
                _db.ExchangeRates.Add(r);
            }
            else
            {
                existing.UpdateRate(r.Rate, r.RetrievedAt);
            }
        }

        var linhasAtualizadas = await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetLatestAsync(string? quoteCurrency, int take, CancellationToken ct)
    {
        take = ClampTake(take);

        var q = _db.ExchangeRates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(quoteCurrency))
            q = q.Where(x => x.QuoteCurrency == quoteCurrency.Trim().ToUpperInvariant());

        return await q
            //.OrderByDescending(x => x.AsOfDate)
            //.ThenByDescending(x => x.RetrievedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetByDateAsync(DateOnly asOfDate, string? quoteCurrency, int take, CancellationToken ct)
    {
        take = ClampTake(take);

        var q = _db.ExchangeRates.AsNoTracking()
            .Where(x => x.AsOfDate == asOfDate);

        if (!string.IsNullOrWhiteSpace(quoteCurrency))
            q = q.Where(x => x.QuoteCurrency == quoteCurrency.Trim().ToUpperInvariant());

        return await q
            .OrderBy(x => x.QuoteCurrency)
            .Take(take)
            .ToListAsync(ct);
    }

    private static int ClampTake(int take)
    {
        if (take <= 0) return 50;
        return Math.Min(take, 500);
    }
}
