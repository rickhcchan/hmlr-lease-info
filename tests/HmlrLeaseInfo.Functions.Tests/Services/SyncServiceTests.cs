namespace HmlrLeaseInfo.Functions.Tests.Services;

using FluentAssertions;
using HmlrLeaseInfo.Core.Configuration;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Models;
using HmlrLeaseInfo.Functions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class SyncServiceTests
{
    private readonly IHmlrClient _hmlrClient = Substitute.For<IHmlrClient>();
    private readonly ILeaseParser _leaseParser = Substitute.For<ILeaseParser>();
    private readonly ILeaseRepository _leaseRepo = Substitute.For<ILeaseRepository>();
    private readonly ISyncMetadataRepository _syncRepo = Substitute.For<ISyncMetadataRepository>();
    private readonly ILogger<SyncService> _logger = Substitute.For<ILogger<SyncService>>();
    private readonly SyncOptions _syncOptions = new()
    {
        DataFreshness = TimeSpan.FromMinutes(30),
        RequestThrottle = TimeSpan.FromMinutes(5)
    };

    private SyncService CreateService() =>
        new(_hmlrClient, _leaseParser, _leaseRepo, _syncRepo,
            Options.Create(_syncOptions), _logger);

    /// <summary>
    /// When last sync completed within DataFreshness, skip the sync entirely.
    /// </summary>
    [Fact]
    public async Task SyncAsync_CompletedAtWithinDataFreshness_SkipsSync()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new SyncMetadata(
                CompletedAt: DateTime.UtcNow.AddMinutes(-10),
                EntriesProcessed: 5,
                ErrorMessage: null));

        var service = CreateService();
        await service.SyncAsync();

        await _hmlrClient.DidNotReceive()
            .GetSchedulesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When no sync has ever occurred, proceed with sync.
    /// </summary>
    [Fact]
    public async Task SyncAsync_NeverSynced_ProceedsWithSync()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);
        _hmlrClient.GetSchedulesAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<RawNoticeOfLease>());

        var service = CreateService();
        await service.SyncAsync();

        await _hmlrClient.Received(1)
            .GetSchedulesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When last sync is older than DataFreshness, proceed with sync.
    /// </summary>
    [Fact]
    public async Task SyncAsync_CompletedAtOlderThanDataFreshness_ProceedsWithSync()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new SyncMetadata(
                CompletedAt: DateTime.UtcNow.AddMinutes(-60),
                EntriesProcessed: 5,
                ErrorMessage: null));
        _hmlrClient.GetSchedulesAsync(Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<RawNoticeOfLease>());

        var service = CreateService();
        await service.SyncAsync();

        await _hmlrClient.Received(1)
            .GetSchedulesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Fetches raw entries, parses each one, and upserts all parsed results.
    /// </summary>
    [Fact]
    public async Task SyncAsync_FetchesSchedules_ParsesAll_UpsertsToRepo()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);

        var raw1 = CreateRawEntry("1");
        var raw2 = CreateRawEntry("2");
        _hmlrClient.GetSchedulesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { raw1, raw2 });

        var parsed1 = CreateParsedEntry("TGL1");
        var parsed2 = CreateParsedEntry("TGL2");
        _leaseParser.Parse(raw1).Returns(parsed1);
        _leaseParser.Parse(raw2).Returns(parsed2);

        var service = CreateService();
        await service.SyncAsync();

        await _leaseRepo.Received(1).UpsertParsedAsync(
            Arg.Is<IEnumerable<ParsedNoticeOfLease>>(e => e.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// After a successful sync, updates metadata with CompletedAt and entry count.
    /// </summary>
    [Fact]
    public async Task SyncAsync_UpdatesSyncMetadata_WithCompletedAtAndCount()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);

        var raw = CreateRawEntry("1");
        _hmlrClient.GetSchedulesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { raw });
        _leaseParser.Parse(raw).Returns(CreateParsedEntry("TGL1"));

        var service = CreateService();
        await service.SyncAsync();

        await _syncRepo.Received(1).UpdateAsync(
            Arg.Is<SyncMetadata>(m =>
                m.CompletedAt != null &&
                m.EntriesProcessed == 1 &&
                m.ErrorMessage == null),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the HMLR API throws, records the error without setting CompletedAt.
    /// </summary>
    [Fact]
    public async Task SyncAsync_HmlrApiFailure_SetsErrorMessage_DoesNotUpdateCompletedAt()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);
        _hmlrClient.GetSchedulesAsync(Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("HMLR API returned 500"));

        var service = CreateService();
        await service.SyncAsync();

        await _syncRepo.Received(1).UpdateAsync(
            Arg.Is<SyncMetadata>(m =>
                m.CompletedAt == null &&
                m.ErrorMessage != null &&
                m.ErrorMessage.Contains("500")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When one entry fails to parse, the rest are still upserted.
    /// </summary>
    [Fact]
    public async Task SyncAsync_SingleParseFailure_SkipsEntry_ProcessesRest()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);

        var raw1 = CreateRawEntry("1");
        var raw2 = CreateRawEntry("2");
        var raw3 = CreateRawEntry("3");
        _hmlrClient.GetSchedulesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { raw1, raw2, raw3 });

        _leaseParser.Parse(raw1).Returns(CreateParsedEntry("TGL1"));
        _leaseParser.Parse(raw2).Throws(new FormatException("Bad data"));
        _leaseParser.Parse(raw3).Returns(CreateParsedEntry("TGL3"));

        var service = CreateService();
        await service.SyncAsync();

        await _leaseRepo.Received(1).UpsertParsedAsync(
            Arg.Is<IEnumerable<ParsedNoticeOfLease>>(e => e.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When entries are skipped due to parse failures, the error message notes the count.
    /// </summary>
    [Fact]
    public async Task SyncAsync_SingleParseFailure_SetsErrorMessageWithSkippedInfo()
    {
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);

        var raw1 = CreateRawEntry("1");
        var raw2 = CreateRawEntry("2");
        _hmlrClient.GetSchedulesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { raw1, raw2 });

        _leaseParser.Parse(raw1).Returns(CreateParsedEntry("TGL1"));
        _leaseParser.Parse(raw2).Throws(new FormatException("Bad data"));

        var service = CreateService();
        await service.SyncAsync();

        await _syncRepo.Received(1).UpdateAsync(
            Arg.Is<SyncMetadata>(m =>
                m.CompletedAt != null &&
                m.EntriesProcessed == 1 &&
                m.ErrorMessage != null &&
                m.ErrorMessage.Contains("1 skipped")),
            Arg.Any<CancellationToken>());
    }

    private static RawNoticeOfLease CreateRawEntry(string entryNumber) =>
        new(entryNumber, "", "Schedule of Notices of Leases",
            new List<string> { "01.01.2020      Test Property                         01.01.2020      TGL" + entryNumber });

    private static ParsedNoticeOfLease CreateParsedEntry(string lesseesTitle) =>
        new(1, null, "01.01.2020", "Test Property",
            "01.01.2020 99 years from 1.1.2020", lesseesTitle, new List<string>());
}
