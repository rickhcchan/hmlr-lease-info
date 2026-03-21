namespace HmlrLeaseInfo.Infrastructure.Tests.Storage;

using Azure.Data.Tables;
using FluentAssertions;
using HmlrLeaseInfo.Core.Models;
using HmlrLeaseInfo.Infrastructure.Storage;

[Collection("Azurite")]
public class TableStorageLeaseRepositoryTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    private static (TableStorageLeaseRepository Repository, TableClient TableClient) CreateRepository()
    {
        var tableName = $"Leases{Guid.NewGuid():N}";
        var tableClient = new TableClient(ConnectionString, tableName);
        tableClient.CreateIfNotExists();
        return (new TableStorageLeaseRepository(tableClient), tableClient);
    }

    [Fact]
    public async Task GetAsync_NonExistentEntry_ReturnsNull()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var result = await repository.GetAsync("NONEXISTENT");

            result.Should().BeNull();
        }
        finally { await tableClient.DeleteAsync(); }
    }

    [Fact]
    public async Task UpsertParsedAsync_MultipleEntries_AllRetrievable()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var entries = new[]
            {
                CreateTestEntry("TGL513556"),
                CreateTestEntry("TGL383606"),
                CreateTestEntry("TGL24029"),
            };
            await repository.UpsertParsedAsync(entries);

            foreach (var entry in entries)
            {
                var result = await repository.GetAsync(entry.LesseesTitle);
                result.Should().NotBeNull();
                result!.LesseesTitle.Should().Be(entry.LesseesTitle);
            }
        }
        finally { await tableClient.DeleteAsync(); }
    }

    [Fact]
    public async Task UpsertParsedAsync_ExistingEntry_OverwritesCorrectly()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var original = CreateTestEntry("EGL386024", propertyDescription: "Original");
            await repository.UpsertParsedAsync([original]);

            var updated = CreateTestEntry("EGL386024", propertyDescription: "Updated");
            await repository.UpsertParsedAsync([updated]);

            var result = await repository.GetAsync("EGL386024");
            result.Should().NotBeNull();
            result!.PropertyDescription.Should().Be("Updated");
        }
        finally { await tableClient.DeleteAsync(); }
    }

    [Fact]
    public async Task GetAsync_EntryWithNotes_DeserializesNotesFromJson()
    {
        var (repository, tableClient) = CreateRepository();
        try
        {
            var entry = new ParsedNoticeOfLease(
                EntryNumber: 4,
                EntryDate: null,
                RegistrationDateAndPlanRef: "24.07.1989",
                PropertyDescription: "17 Ashworth Close",
                DateOfLeaseAndTerm: "01.06.1989 125 years from 1.6.1989",
                LesseesTitle: "TGL24029N",
                Notes: ["NOTE 1: First note", "NOTE 2: Second note"]
            );
            await repository.UpsertParsedAsync([entry]);

            var result = await repository.GetAsync("TGL24029N");

            result.Should().NotBeNull();
            result!.Notes.Should().HaveCount(2);
            result.Notes[0].Should().Be("NOTE 1: First note");
            result.Notes[1].Should().Be("NOTE 2: Second note");
        }
        finally { await tableClient.DeleteAsync(); }
    }

    private static ParsedNoticeOfLease CreateTestEntry(
        string lesseesTitle,
        string propertyDescription = "Test Property")
    {
        return new ParsedNoticeOfLease(
            EntryNumber: 1,
            EntryDate: null,
            RegistrationDateAndPlanRef: "01.01.2020",
            PropertyDescription: propertyDescription,
            DateOfLeaseAndTerm: "01.01.2020 99 years from 1.1.2020",
            LesseesTitle: lesseesTitle,
            Notes: new List<string>()
        );
    }
}
