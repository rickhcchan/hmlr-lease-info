namespace HmlrLeaseInfo.Infrastructure.Tests.Storage;

using Azure.Data.Tables;
using FluentAssertions;
using HmlrLeaseInfo.Core.Models;
using HmlrLeaseInfo.Infrastructure.Storage;

[Collection("Azurite")]
public class TableStorageSyncMetadataRepositoryTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    private static (TableStorageSyncMetadataRepository Repository, TableClient TableClient) CreateRepository()
    {
        var tableName = $"Sync{Guid.NewGuid():N}";
        var tableClient = new TableClient(ConnectionString, tableName);
        tableClient.CreateIfNotExists();
        return (new TableStorageSyncMetadataRepository(tableClient), tableClient);
    }

    [Fact]
    public async Task GetAsync_NeverSynced_ReturnsNull()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var result = await repository.GetAsync();

            result.Should().BeNull();
        }
        finally { await tableClient.DeleteAsync(); }
    }

    [Fact]
    public async Task UpdateAsync_ThenGetAsync_ReturnsUpdatedMetadata()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var metadata = new SyncMetadata(
                CompletedAt: DateTime.UtcNow,
                EntriesProcessed: 5,
                ErrorMessage: null
            );
            await repository.UpdateAsync(metadata);

            var result = await repository.GetAsync();

            result.Should().NotBeNull();
            result!.EntriesProcessed.Should().Be(5);
            result.ErrorMessage.Should().BeNull();
        }
        finally { await tableClient.DeleteAsync(); }
    }

    [Fact]
    public async Task UpdateAsync_SetsCompletedAt_CorrectTimestamp()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var now = DateTime.UtcNow;
            var metadata = new SyncMetadata(
                CompletedAt: now,
                EntriesProcessed: 3,
                ErrorMessage: null
            );
            await repository.UpdateAsync(metadata);

            var result = await repository.GetAsync();

            result.Should().NotBeNull();
            result!.CompletedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        }
        finally { await tableClient.DeleteAsync(); }
    }

    [Fact]
    public async Task UpdateAsync_WithError_PersistsErrorMessage()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var metadata = new SyncMetadata(
                CompletedAt: null,
                EntriesProcessed: 0,
                ErrorMessage: "HMLR API returned 500"
            );
            await repository.UpdateAsync(metadata);

            var result = await repository.GetAsync();

            result.Should().NotBeNull();
            result!.CompletedAt.Should().BeNull();
            result.ErrorMessage.Should().Be("HMLR API returned 500");
        }
        finally { await tableClient.DeleteAsync(); }
    }
}
