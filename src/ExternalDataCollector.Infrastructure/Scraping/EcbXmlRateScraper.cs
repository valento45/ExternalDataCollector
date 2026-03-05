using System.Globalization;
using System.Xml.Linq;
using ExternalDataCollector.Application.Abstractions;

namespace ExternalDataCollector.Infrastructure.Scraping;

public sealed class EcbXmlRateScraper : IRateScraper
{
    private readonly HttpClient _http;
    private readonly string[] _wantedQuotes;

    private const string BaseCurrency = "EUR";
    private const string DefaultUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";

    public EcbXmlRateScraper(HttpClient http, string[] wantedQuotes)
    {
        _http = http;
        _wantedQuotes = wantedQuotes.Length == 0
            ? ["USD", "BRL", "GBP"]
            : wantedQuotes.Select(x => x.Trim().ToUpperInvariant()).Distinct().ToArray();
    }

    public async Task<IReadOnlyList<ScrapedRate>> ScrapeAsync(CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, DefaultUrl);
        req.Headers.UserAgent.ParseAdd("ExternalDataCollector/1.0 (+worker)");

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var xml = await resp.Content.ReadAsStringAsync(ct);

        var doc = XDocument.Parse(xml);

        var parsed = TryParseStandard(doc) ?? TryParseFallback(doc)
            ?? throw new InvalidOperationException("Layout inesperado no XML do ECB (não foi possível localizar nós de cotação).");

        var asOf = parsed.asOf;
        var cubes = parsed.cubes;

        var dict = cubes
            .Where(x => !string.IsNullOrWhiteSpace(x.currency) && x.rate > 0)
            .ToDictionary(x => x.currency.ToUpperInvariant(), x => x.rate);

        var result = new List<ScrapedRate>();
        foreach (var q in _wantedQuotes)
        {
            if (dict.TryGetValue(q, out var rate))
                result.Add(new ScrapedRate(BaseCurrency, q, rate, asOf));
        }

        if (result.Count == 0)
            throw new InvalidOperationException("Nenhuma cotação encontrada para as moedas desejadas. Possível mudança no feed ou moedas indisponíveis.");

        return result;
    }

    private static (DateOnly asOf, List<(string currency, decimal rate)> cubes)? TryParseStandard(XDocument doc)
    {
        var timeNode = doc.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "Cube" && x.Attribute("time") is not null);

        var timeAttr = timeNode?.Attribute("time")?.Value;
        if (string.IsNullOrWhiteSpace(timeAttr)) return null;

        if (!DateOnly.TryParseExact(timeAttr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOf))
            return null;

        var rates = timeNode!.Descendants()
            .Where(x => x.Name.LocalName == "Cube" && x.Attribute("currency") is not null && x.Attribute("rate") is not null)
            .Select(x =>
            {
                var cur = x.Attribute("currency")!.Value;
                var rateStr = x.Attribute("rate")!.Value;
                if (!decimal.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                    rate = 0;
                return (currency: cur, rate);
            })
            .ToList();

        return rates.Count == 0 ? null : (asOf, rates);
    }

    private static (DateOnly asOf, List<(string currency, decimal rate)> cubes)? TryParseFallback(XDocument doc)
    {
        var timeAttr = doc.Descendants()
            .Select(x => x.Attribute("time")?.Value)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        if (string.IsNullOrWhiteSpace(timeAttr)) return null;

        if (!DateOnly.TryParseExact(timeAttr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOf))
            return null;

        var rates = doc.Descendants()
            .Where(x => x.Attribute("currency") is not null && x.Attribute("rate") is not null)
            .Select(x =>
            {
                var cur = x.Attribute("currency")!.Value;
                var rateStr = x.Attribute("rate")!.Value;
                if (!decimal.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                    rate = 0;
                return (currency: cur, rate);
            })
            .ToList();

        return rates.Count == 0 ? null : (asOf, rates);
    }
}
