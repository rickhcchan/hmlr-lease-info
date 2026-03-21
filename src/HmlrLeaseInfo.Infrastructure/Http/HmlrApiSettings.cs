namespace HmlrLeaseInfo.Infrastructure.Http;

/// <summary>
/// Configuration for connecting to the HMLR mock API.
/// </summary>
public class HmlrApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
