namespace HmlrLeaseInfo.Api.Services;

using Azure.Storage.Queues;
using HmlrLeaseInfo.Api.Interfaces;
using HmlrLeaseInfo.Api.Models;
using HmlrLeaseInfo.Core.Configuration;
using HmlrLeaseInfo.Core.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

/// <summary>
/// Orchestrates lease lookup with caching, sync triggering, and response selection.
/// Returns 200 (found), 202 (syncing), or 404 (fresh data, not found).
/// </summary>
public class LeaseService(
    HybridCache cache,
    ILeaseRepository leaseRepository,
    ISyncMetadataRepository syncMetadataRepository,
    QueueClient queueClient,
    IOptions<SyncOptions> syncOptions) : ILeaseService
{
    public async Task<IResult> GetLeaseAsync(string titleNumber, CancellationToken cancellationToken = default)
    {
        var options = syncOptions.Value;

        var lease = await cache.GetOrCreateAsync(
            $"lease:{titleNumber}",
            async ct => await leaseRepository.GetAsync(titleNumber, ct),
            new HybridCacheEntryOptions { Expiration = options.DataFreshness },
            cancellationToken: cancellationToken);

        if (lease is not null)
            return Results.Ok(lease);

        var syncMetadata = await syncMetadataRepository.GetAsync(cancellationToken);

        if (syncMetadata?.CompletedAt is not null
            && DateTime.UtcNow - syncMetadata.CompletedAt.Value < options.DataFreshness)
        {
            return Results.NotFound(new LeaseResponse(
                "Entry not present as of last sync.",
                LastSyncAt: syncMetadata.CompletedAt));
        }

        await queueClient.SendMessageAsync("sync", cancellationToken);
        return Results.Accepted(value: new LeaseResponse(
            "Data is being synced. Please retry shortly."));
    }
}
