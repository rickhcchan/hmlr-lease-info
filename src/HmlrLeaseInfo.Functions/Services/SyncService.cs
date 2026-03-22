namespace HmlrLeaseInfo.Functions.Services;

using HmlrLeaseInfo.Core.Configuration;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Functions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Fetches HMLR schedule data, parses entries, and persists results with freshness gating.
/// </summary>
public class SyncService(
    IHmlrClient hmlrClient,
    ILeaseParser leaseParser,
    ILeaseRepository leaseRepository,
    ISyncMetadataRepository syncMetadataRepository,
    IOptions<SyncOptions> syncOptions,
    ILogger<SyncService> logger) : ISyncService
{
    /// <summary>
    /// Checks freshness gate, fetches and parses HMLR data, then persists results.
    /// </summary>
    public Task SyncAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
