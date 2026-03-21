namespace HmlrLeaseInfo.Api.Services;

using Azure.Storage.Queues;
using HmlrLeaseInfo.Api.Interfaces;
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
    /// <inheritdoc />
    public Task<IResult> GetLeaseAsync(string titleNumber, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
