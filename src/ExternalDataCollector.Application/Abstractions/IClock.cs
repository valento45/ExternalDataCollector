namespace ExternalDataCollector.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
