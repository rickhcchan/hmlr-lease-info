namespace HmlrLeaseInfo.Infrastructure.Storage;

using Azure;
using Azure.Data.Tables;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Models;

/// <summary>
/// Azure Table Storage implementation of ILeaseRepository.
/// </summary>
public class TableStorageLeaseRepository(TableClient tableClient) : ILeaseRepository
{
    public async Task<ParsedNoticeOfLease?> GetAsync(string lesseesTitle, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await tableClient.GetEntityAsync<LeaseTableEntity>(
                "lease", lesseesTitle, cancellationToken: cancellationToken);
            return response.Value.ToDomain();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task UpsertParsedAsync(IEnumerable<ParsedNoticeOfLease> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            var entity = LeaseTableEntity.FromDomain(entry);
            await tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        }
    }
}
