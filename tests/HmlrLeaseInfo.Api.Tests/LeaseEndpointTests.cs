namespace HmlrLeaseInfo.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HmlrLeaseInfo.Api.Interfaces;
using HmlrLeaseInfo.Api.Models;
using HmlrLeaseInfo.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

/// <summary>
/// Integration tests for GET /{titleNumber} endpoint.
/// Uses WebApplicationFactory with mocked ILeaseService to test HTTP behavior.
/// </summary>
public class LeaseEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LeaseEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithService(ILeaseService leaseService)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => leaseService);
            });
        }).CreateClient();
    }

    // 200 path

    [Fact]
    public async Task GetTitleNumber_ExistsInRepo_Returns200()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var parsed = CreateTestEntry("EGL557357");
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Ok(parsed));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/EGL557357");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTitleNumber_ExistsInRepo_ResponseMatchesParsedData()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var parsed = CreateTestEntry("EGL557357");
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Ok(parsed));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/EGL557357");

        var result = await response.Content.ReadFromJsonAsync<ParsedNoticeOfLease>();
        result.Should().NotBeNull();
        result!.LesseesTitle.Should().Be("EGL557357");
        result.EntryNumber.Should().Be(1);
        result.Notes.Should().BeEmpty();
    }

    // 404 path (fresh data, title absent)

    [Fact]
    public async Task GetTitleNumber_NotInRepo_FreshSync_Returns404()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var leaseResponse = new LeaseResponse(
            "Entry not present as of last sync.",
            LastSyncAt: DateTime.UtcNow);
        leaseService.GetLeaseAsync("NONEXISTENT", Arg.Any<CancellationToken>())
            .Returns(Results.NotFound(leaseResponse));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/NONEXISTENT");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTitleNumber_404Response_ContainsLastSyncAt()
    {
        var syncTime = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc);
        var leaseService = Substitute.For<ILeaseService>();
        var leaseResponse = new LeaseResponse(
            "Entry not present as of last sync.",
            LastSyncAt: syncTime);
        leaseService.GetLeaseAsync("NONEXISTENT", Arg.Any<CancellationToken>())
            .Returns(Results.NotFound(leaseResponse));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/NONEXISTENT");

        var result = await response.Content.ReadFromJsonAsync<LeaseResponse>();
        result.Should().NotBeNull();
        result!.LastSyncAt.Should().NotBeNull();
        result.Message.Should().Contain("not present");
    }

    // 202 path (never synced)

    [Fact]
    public async Task GetTitleNumber_NotInRepo_NeverSynced_Returns202()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var leaseResponse = new LeaseResponse("Data is being synced. Please retry shortly.");
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Accepted(value: leaseResponse));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/EGL557357");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GetTitleNumber_202Response_ContainsProcessingMessage()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var leaseResponse = new LeaseResponse("Data is being synced. Please retry shortly.");
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Accepted(value: leaseResponse));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/EGL557357");

        var result = await response.Content.ReadFromJsonAsync<LeaseResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("synced");
    }

    // 202 path (stale data — re-sync)

    [Fact]
    public async Task GetTitleNumber_NotInRepo_StaleSync_Returns202()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var leaseResponse = new LeaseResponse("Data is being synced. Please retry shortly.");
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Accepted(value: leaseResponse));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/EGL557357");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
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
