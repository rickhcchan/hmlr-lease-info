namespace HmlrLeaseInfo.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HmlrLeaseInfo.Api.Interfaces;
using HmlrLeaseInfo.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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

    [Fact]
    public async Task GetTitleNumber_RoutesToEndpoint_Returns200()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var parsed = new ParsedNoticeOfLease(1, null, "01.01.2020", "Property", "Lease term", "EGL557357", new List<string>());
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Ok(parsed));

        var client = CreateClientWithService(leaseService);
        var response = await client.GetAsync("/EGL557357");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTitleNumber_200Response_SerializesParsedData()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var parsed = new ParsedNoticeOfLease(1, null, "01.01.2020", "Property", "Lease term", "EGL557357", new List<string>());
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Ok(parsed));

        var client = CreateClientWithService(leaseService);
        var result = await client.GetFromJsonAsync<ParsedNoticeOfLease>("/EGL557357");

        result.Should().NotBeNull();
        result!.LesseesTitle.Should().Be("EGL557357");
    }
}
