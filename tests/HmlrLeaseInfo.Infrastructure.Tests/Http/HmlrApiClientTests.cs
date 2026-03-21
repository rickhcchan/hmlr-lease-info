namespace HmlrLeaseInfo.Infrastructure.Tests.Http;

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HmlrLeaseInfo.Core.Models;
using HmlrLeaseInfo.Infrastructure.Http;
using Microsoft.Extensions.Options;

public class HmlrApiClientTests
{
    private readonly HmlrApiSettings _settings = new()
    {
        BaseUrl = "https://localhost:7273",
        Username = "username",
        Password = "password"
    };

    [Fact]
    public async Task GetSchedulesAsync_SetsBasicAuthHeader_WithConfiguredCredentials()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        });

        var client = CreateClient(handler);
        await client.GetSchedulesAsync();

        capturedRequest.Should().NotBeNull();
        var authHeader = capturedRequest!.Headers.Authorization;
        authHeader.Should().NotBeNull();
        authHeader!.Scheme.Should().Be("Basic");

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter!));
        decoded.Should().Be("username:password");
    }

    [Fact]
    public async Task GetSchedulesAsync_Returns200_DeserializesRawSchedules()
    {
        var rawData = new[]
        {
            new { EntryNumber = "1", EntryDate = "", EntryType = "Schedule of Notices of Leases", EntryText = new[] { "line1" } }
        };
        var json = JsonSerializer.Serialize(rawData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var handler = new FakeHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handler);
        var result = (await client.GetSchedulesAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].EntryNumber.Should().Be("1");
        result[0].EntryText.Should().Contain("line1");
    }

    [Fact]
    public async Task GetSchedulesAsync_ReturnsNon200_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = CreateClient(handler);

        var act = () => client.GetSchedulesAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private HmlrApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_settings.BaseUrl) };
        var options = Options.Create(_settings);
        return new HmlrApiClient(httpClient, options);
    }

    /// <summary>
    /// Simple fake HTTP handler for unit testing without real HTTP calls.
    /// </summary>
    private class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
