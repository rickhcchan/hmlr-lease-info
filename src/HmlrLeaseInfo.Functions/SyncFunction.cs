namespace HmlrLeaseInfo.Functions;

using HmlrLeaseInfo.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;

/// <summary>
/// Queue-triggered function that delegates to SyncService for HMLR data synchronisation.
/// </summary>
public class SyncFunction(ISyncService syncService)
{
    [Function("SyncFunction")]
    public async Task Run(
        [QueueTrigger("sync-requests")] string message,
        CancellationToken cancellationToken)
    {
        await syncService.SyncAsync(cancellationToken);
    }
}
