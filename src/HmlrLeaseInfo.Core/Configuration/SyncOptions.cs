namespace HmlrLeaseInfo.Core.Configuration;

public record SyncOptions
{
    public TimeSpan DataFreshness { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan RequestThrottle { get; init; } = TimeSpan.FromMinutes(5);
}
