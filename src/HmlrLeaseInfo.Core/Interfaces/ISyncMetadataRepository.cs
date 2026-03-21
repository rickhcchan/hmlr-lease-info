using HmlrLeaseInfo.Core.Models;

namespace HmlrLeaseInfo.Core.Interfaces;

/// <summary>
/// Persistence layer for sync state tracking.
/// </summary>
public interface ISyncMetadataRepository
{
    /// <summary>
    /// Retrieves the current sync metadata, or null if no sync has occurred.
    /// </summary>
    Task<SyncMetadata?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the sync metadata after a sync completes or fails.
    /// </summary>
    Task UpdateAsync(SyncMetadata metadata, CancellationToken cancellationToken = default);
}
