namespace HmlrLeaseInfo.Core.Configuration;

/// <summary>
/// Configuration for sync timing and data freshness thresholds.
/// </summary>
public record SyncOptions
{
    /// <summary>Maximum age of cached data before re-fetching from the HMLR API.</summary>
    public TimeSpan DataFreshness { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Minimum interval between sync attempts to avoid excessive API calls.</summary>
    public TimeSpan RequestThrottle { get; init; } = TimeSpan.FromMinutes(5);
}
