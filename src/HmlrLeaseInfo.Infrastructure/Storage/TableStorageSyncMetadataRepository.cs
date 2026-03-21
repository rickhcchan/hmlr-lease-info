namespace HmlrLeaseInfo.Infrastructure.Storage;

using Azure;
using Azure.Data.Tables;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Models;

/// <summary>
/// Azure Table Storage implementation of ISyncMetadataRepository.
/// Single-row read/write for sync state tracking.
/// </summary>
public class TableStorageSyncMetadataRepository(TableClient tableClient) : ISyncMetadataRepository
{
    public async Task<SyncMetadata?> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await tableClient.GetEntityAsync<SyncMetadataTableEntity>(
                "sync", "latest", cancellationToken: cancellationToken);
            return response.Value.ToDomain();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task UpdateAsync(SyncMetadata metadata, CancellationToken cancellationToken = default)
    {
        var entity = SyncMetadataTableEntity.FromDomain(metadata);
        await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
    }
}
