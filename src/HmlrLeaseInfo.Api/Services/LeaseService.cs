namespace HmlrLeaseInfo.Api.Services;

using Azure.Storage.Queues;
using HmlrLeaseInfo.Api.Interfaces;
using HmlrLeaseInfo.Api.Models;
using HmlrLeaseInfo.Core.Configuration;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Models;
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
    // Flow:
    // 1. Cache/repo lookup → found → 200 (silently re-syncs if stale)
    // 2. Not found + sync fresh → 404
    // 3. Not found + sync stale/missing → enqueue sync → 202
    public async Task<IResult> GetLeaseAsync(string titleNumber, CancellationToken cancellationToken = default)
    {
        var options = syncOptions.Value;

        var lease = await cache.GetOrCreateAsync(
            $"lease:{titleNumber}",
            async ct => await leaseRepository.GetAsync(titleNumber, ct),
            new HybridCacheEntryOptions { Expiration = options.DataFreshness },
            cancellationToken: cancellationToken);

        if (lease is not null)
        {
            await EnqueueIfSyncStale(options, cancellationToken);
            return Results.Ok(lease);
        }

        // HybridCache caches null by default — remove so next call re-queries the repo
        await cache.RemoveAsync($"lease:{titleNumber}", cancellationToken);

        var syncMetadata = await syncMetadataRepository.GetAsync(cancellationToken);

        if (!IsSyncStale(syncMetadata, options))
        {
            return Results.NotFound(new LeaseResponse(
                "Entry not present as of last sync.",
                LastSyncAt: syncMetadata!.CompletedAt));
        }

        await EnqueueSyncThrottled(options, cancellationToken);

        return Results.Accepted(value: new LeaseResponse(
            "Data is being synced. Please retry shortly."));
    }

    private static bool IsSyncStale(SyncMetadata? syncMetadata, SyncOptions options) =>
        syncMetadata?.CompletedAt is null
        || DateTime.UtcNow - syncMetadata.CompletedAt.Value >= options.DataFreshness;

    private async Task EnqueueSyncThrottled(SyncOptions options, CancellationToken cancellationToken)
    {
        await cache.GetOrCreateAsync(
            "sync-requested",
            async ct =>
            {
                await queueClient.SendMessageAsync("sync", ct);
                return true;
            },
            new HybridCacheEntryOptions { Expiration = options.RequestThrottle },
            cancellationToken: cancellationToken);
    }

    private async Task EnqueueIfSyncStale(SyncOptions options, CancellationToken cancellationToken)
    {
        await cache.GetOrCreateAsync(
            "sync-requested",
            async ct =>
            {
                var syncMetadata = await syncMetadataRepository.GetAsync(ct);
                if (IsSyncStale(syncMetadata, options))
                    await queueClient.SendMessageAsync("sync", ct);
                return true;
            },
            new HybridCacheEntryOptions { Expiration = options.RequestThrottle },
            cancellationToken: cancellationToken);
    }
}
