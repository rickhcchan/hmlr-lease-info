namespace HmlrLeaseInfo.Api.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

    private static AuthenticationHeaderValue BasicAuth(string user, string pass) =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}")));

    [Fact]
    public async Task GetTitleNumber_NoAuthHeader_Returns401()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var client = CreateClientWithService(leaseService);

        var response = await client.GetAsync("/EGL557357");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTitleNumber_WrongCredentials_Returns401()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var client = CreateClientWithService(leaseService);

        var request = new HttpRequestMessage(HttpMethod.Get, "/EGL557357");
        request.Headers.Authorization = BasicAuth("wrong", "creds");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTitleNumber_ValidCredentials_Returns200()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var parsed = new ParsedNoticeOfLease(1, null, "01.01.2020", "Property", "Lease term", "EGL557357", new List<string>());
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Ok(parsed));

        var client = CreateClientWithService(leaseService);

        var request = new HttpRequestMessage(HttpMethod.Get, "/EGL557357");
        request.Headers.Authorization = BasicAuth("username", "password");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTitleNumber_ValidCredentials_SerializesParsedData()
    {
        var leaseService = Substitute.For<ILeaseService>();
        var parsed = new ParsedNoticeOfLease(1, null, "01.01.2020", "Property", "Lease term", "EGL557357", new List<string>());
        leaseService.GetLeaseAsync("EGL557357", Arg.Any<CancellationToken>())
            .Returns(Results.Ok(parsed));

        var client = CreateClientWithService(leaseService);

        var request = new HttpRequestMessage(HttpMethod.Get, "/EGL557357");
        request.Headers.Authorization = BasicAuth("username", "password");
        var response = await client.SendAsync(request);

        var result = await response.Content.ReadFromJsonAsync<ParsedNoticeOfLease>();
        result.Should().NotBeNull();
        result!.LesseesTitle.Should().Be("EGL557357");
    }
}
