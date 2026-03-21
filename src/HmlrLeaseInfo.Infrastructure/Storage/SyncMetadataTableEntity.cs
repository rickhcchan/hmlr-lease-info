namespace HmlrLeaseInfo.Infrastructure.Storage;

using Azure;
using Azure.Data.Tables;
using HmlrLeaseInfo.Core.Models;

/// <summary>
/// Azure Table Storage entity for sync state tracking.
/// Single row: PartitionKey="sync", RowKey="latest".
/// </summary>
public class SyncMetadataTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "sync";
    public string RowKey { get; set; } = "latest";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public DateTime? CompletedAt { get; set; }
    public int EntriesProcessed { get; set; }
    public string? ErrorMessage { get; set; }

    public static SyncMetadataTableEntity FromDomain(SyncMetadata metadata)
    {
        return new SyncMetadataTableEntity
        {
            CompletedAt = metadata.CompletedAt,
            EntriesProcessed = metadata.EntriesProcessed,
            ErrorMessage = metadata.ErrorMessage
        };
    }

    public SyncMetadata ToDomain()
    {
        return new SyncMetadata(
            CompletedAt: CompletedAt,
            EntriesProcessed: EntriesProcessed,
            ErrorMessage: ErrorMessage
        );
    }
}
