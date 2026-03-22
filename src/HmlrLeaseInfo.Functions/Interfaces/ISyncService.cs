namespace HmlrLeaseInfo.Functions.Interfaces;

/// <summary>
/// Orchestrates data synchronisation from the HMLR API to Table Storage.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Fetches, parses, and persists HMLR schedule data if the current data is stale.
    /// </summary>
    Task SyncAsync(CancellationToken cancellationToken = default);
}
