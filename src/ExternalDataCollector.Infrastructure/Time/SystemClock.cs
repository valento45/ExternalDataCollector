using ExternalDataCollector.Application.Abstractions;

namespace ExternalDataCollector.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
