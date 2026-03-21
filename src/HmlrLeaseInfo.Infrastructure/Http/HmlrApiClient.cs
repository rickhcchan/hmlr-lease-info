namespace HmlrLeaseInfo.Infrastructure.Http;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Models;
using Microsoft.Extensions.Options;

/// <summary>
/// HTTP client for fetching raw schedule data from the HMLR mock API.
/// Uses Basic Auth with the configured credentials.
/// </summary>
public class HmlrApiClient(HttpClient httpClient, IOptions<HmlrApiSettings> options) : IHmlrClient
{
    /// <summary>
    /// Fetches all raw schedule entries from GET /schedules.
    /// </summary>
    public async Task<IEnumerable<RawNoticeOfLease>> GetSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));

        var request = new HttpRequestMessage(HttpMethod.Get, "schedules");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<RawNoticeOfLease>>(cancellationToken: cancellationToken)
            ?? Enumerable.Empty<RawNoticeOfLease>();
    }
}
