namespace HmlrLeaseInfo.Api.Tests.Services;

using Azure.Storage.Queues;
using FluentAssertions;
using HmlrLeaseInfo.Api.Models;
using HmlrLeaseInfo.Api.Services;
using HmlrLeaseInfo.Core.Configuration;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

/// <summary>
/// Unit tests for LeaseService decision logic.
/// Mocks repos and queue, uses real in-memory HybridCache.
/// </summary>
public class LeaseServiceTests
{
    private readonly ILeaseRepository _leaseRepo = Substitute.For<ILeaseRepository>();
    private readonly ISyncMetadataRepository _syncRepo = Substitute.For<ISyncMetadataRepository>();
    private readonly QueueClient _queueClient = Substitute.For<QueueClient>();
    private readonly SyncOptions _syncOptions = new()
    {
        DataFreshness = TimeSpan.FromMinutes(30),
        RequestThrottle = TimeSpan.FromMinutes(5)
    };

    private LeaseService CreateService()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();

        return new LeaseService(
            cache,
            _leaseRepo,
            _syncRepo,
            _queueClient,
            Options.Create(_syncOptions));
    }

    // 200 path — lease found

    [Fact]
    public async Task GetLeaseAsync_RepoHasEntry_Returns200WithData()
    {
        var entry = CreateTestEntry("EGL557357");
        _leaseRepo.GetAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(entry);

        var service = CreateService();
        var result = await service.GetLeaseAsync("EGL557357");

        var ok = result.Should().BeOfType<Ok<ParsedNoticeOfLease>>().Subject;
        ok.Value!.LesseesTitle.Should().Be("EGL557357");
    }

    // 404 path — fresh sync, title absent

    [Fact]
    public async Task GetLeaseAsync_NotInRepo_FreshSync_Returns404WithLastSyncAt()
    {
        var syncTime = DateTime.UtcNow;
        _leaseRepo.GetAsync("MISSING", Arg.Any<CancellationToken>())
            .Returns((ParsedNoticeOfLease?)null);
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new SyncMetadata(
                CompletedAt: syncTime,
                EntriesProcessed: 5,
                ErrorMessage: null));

        var service = CreateService();
        var result = await service.GetLeaseAsync("MISSING");

        var notFound = result.Should().BeOfType<NotFound<LeaseResponse>>().Subject;
        notFound.Value!.LastSyncAt.Should().Be(syncTime);
        notFound.Value.Message.Should().Contain("not present");
    }

    // 202 path — never synced

    [Fact]
    public async Task GetLeaseAsync_NotInRepo_NeverSynced_Returns202()
    {
        _leaseRepo.GetAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns((ParsedNoticeOfLease?)null);
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);

        var service = CreateService();
        var result = await service.GetLeaseAsync("EGL557357");

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(202);
    }

    [Fact]
    public async Task GetLeaseAsync_NeverSynced_EnqueuesSyncMessage()
    {
        _leaseRepo.GetAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns((ParsedNoticeOfLease?)null);
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns((SyncMetadata?)null);

        var service = CreateService();
        await service.GetLeaseAsync("EGL557357");

        await _queueClient.Received().SendMessageAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // 202 path — stale sync

    [Fact]
    public async Task GetLeaseAsync_NotInRepo_StaleSync_Returns202()
    {
        _leaseRepo.GetAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns((ParsedNoticeOfLease?)null);
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new SyncMetadata(
                CompletedAt: DateTime.UtcNow.AddMinutes(-60),
                EntriesProcessed: 5,
                ErrorMessage: null));

        var service = CreateService();
        var result = await service.GetLeaseAsync("EGL557357");

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(202);
    }

    [Fact]
    public async Task GetLeaseAsync_StaleSync_EnqueuesSyncMessage()
    {
        _leaseRepo.GetAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns((ParsedNoticeOfLease?)null);
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new SyncMetadata(
                CompletedAt: DateTime.UtcNow.AddMinutes(-60),
                EntriesProcessed: 5,
                ErrorMessage: null));

        var service = CreateService();
        await service.GetLeaseAsync("EGL557357");

        await _queueClient.Received().SendMessageAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Fresh sync should NOT enqueue

    [Fact]
    public async Task GetLeaseAsync_FreshSync_DoesNotEnqueue()
    {
        _leaseRepo.GetAsync("MISSING", Arg.Any<CancellationToken>())
            .Returns((ParsedNoticeOfLease?)null);
        _syncRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new SyncMetadata(
                CompletedAt: DateTime.UtcNow,
                EntriesProcessed: 5,
                ErrorMessage: null));

        var service = CreateService();
        await service.GetLeaseAsync("MISSING");

        await _queueClient.DidNotReceive().SendMessageAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static ParsedNoticeOfLease CreateTestEntry(string lesseesTitle)
    {
        return new ParsedNoticeOfLease(
            EntryNumber: 1,
            EntryDate: null,
            RegistrationDateAndPlanRef: "01.01.2020",
            PropertyDescription: "Test Property",
            DateOfLeaseAndTerm: "01.01.2020 99 years from 1.1.2020",
            LesseesTitle: lesseesTitle,
            Notes: new List<string>());
    }
}
