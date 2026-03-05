namespace ExternalDataCollector.Domain.Entities;

public sealed class ExchangeRate
{
    public long Id { get; private set; }

    public string BaseCurrency { get; private set; } = default!;
    public string QuoteCurrency { get; private set; } = default!;
    public decimal Rate { get; private set; }

    public DateOnly AsOfDate { get; private set; }          // data do rate (do feed)
    public DateTimeOffset RetrievedAt { get; private set; }  // quando coletou

    private ExchangeRate() { } // EF

    public ExchangeRate(string baseCurrency, string quoteCurrency, decimal rate, DateOnly asOfDate, DateTimeOffset retrievedAt)
    {
        BaseCurrency = Normalize(baseCurrency);
        QuoteCurrency = Normalize(quoteCurrency);
        Rate = rate;
        AsOfDate = asOfDate;
        RetrievedAt = retrievedAt;

        Validate();
    }

    public void UpdateRate(decimal newRate, DateTimeOffset retrievedAt)
    {
        if (newRate <= 0) throw new ArgumentOutOfRangeException(nameof(newRate), "Rate deve ser > 0.");
        Rate = newRate;
        RetrievedAt = retrievedAt;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseCurrency)) throw new ArgumentException("BaseCurrency inválido.");
        if (string.IsNullOrWhiteSpace(QuoteCurrency)) throw new ArgumentException("QuoteCurrency inválido.");
        if (Rate <= 0) throw new ArgumentOutOfRangeException(nameof(Rate), "Rate deve ser > 0.");
    }

    private static string Normalize(string c) => c.Trim().ToUpperInvariant();
}
