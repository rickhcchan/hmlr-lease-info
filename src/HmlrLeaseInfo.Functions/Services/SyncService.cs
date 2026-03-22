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
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var options = syncOptions.Value;
        var metadata = await syncMetadataRepository.GetAsync(cancellationToken);

        if (metadata?.CompletedAt is not null
            && DateTime.UtcNow - metadata.CompletedAt.Value < options.DataFreshness)
        {
            logger.LogInformation("Sync skipped — last sync completed {Ago} ago", DateTime.UtcNow - metadata.CompletedAt.Value);
            return;
        }

        try
        {
            var rawEntries = await hmlrClient.GetSchedulesAsync(cancellationToken);

            var parsed = new List<Core.Models.ParsedNoticeOfLease>();
            var skipped = 0;

            foreach (var raw in rawEntries)
            {
                try
                {
                    parsed.Add(leaseParser.Parse(raw));
                }
                catch (Exception ex)
                {
                    skipped++;
                    logger.LogWarning(ex, "Failed to parse entry {EntryNumber}, skipping", raw.EntryNumber);
                }
            }

            await leaseRepository.UpsertParsedAsync(parsed, cancellationToken);

            await syncMetadataRepository.UpdateAsync(
                new Core.Models.SyncMetadata(
                    CompletedAt: DateTime.UtcNow,
                    EntriesProcessed: parsed.Count,
                    ErrorMessage: skipped > 0 ? $"{skipped} skipped due to parse errors" : null),
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HMLR API call failed");
            await syncMetadataRepository.UpdateAsync(
                new Core.Models.SyncMetadata(
                    CompletedAt: null,
                    EntriesProcessed: 0,
                    ErrorMessage: ex.Message),
                cancellationToken);
        }
    }
}
