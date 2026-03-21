namespace HmlrLeaseInfo.Api.Interfaces;

/// <summary>
/// Orchestrates lease lookup with caching, sync triggering, and response selection.
/// </summary>
public interface ILeaseService
{
    /// <summary>
    /// Retrieves a lease by title number, returning the appropriate HTTP result
    /// (200 with data, 202 if syncing, 404 if not found after fresh sync).
    /// </summary>
    Task<IResult> GetLeaseAsync(string titleNumber, CancellationToken cancellationToken = default);
}
