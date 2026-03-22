namespace HmlrLeaseInfo.Api.Configuration;

/// <summary>
/// Credentials for Basic Auth on the API.
/// </summary>
public record AuthSettings
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
