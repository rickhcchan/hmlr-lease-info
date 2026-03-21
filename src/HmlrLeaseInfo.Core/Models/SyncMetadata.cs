namespace HmlrLeaseInfo.Core.Models;

/// <summary>
/// Tracks the state of the most recent HMLR data sync.
/// </summary>
public record SyncMetadata(
    /// <summary>When the last successful sync completed, or null if never synced.</summary>
    DateTime? CompletedAt,
    /// <summary>Number of entries parsed in the last sync.</summary>
    int EntriesProcessed,
    /// <summary>Error details if the last sync encountered issues.</summary>
    string? ErrorMessage
);
