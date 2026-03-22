namespace HmlrLeaseInfo.Api.Interfaces;

/// <summary>
/// Orchestrates lease lookup with caching, sync triggering, and response selection.
/// </summary>
public interface ILeaseService
{
    /// <summary>
    /// Retrieves a lease by title number. Returns an IResult to allow multiple
    /// response types depending on data availability and sync state.
    /// </summary>
    Task<IResult> GetLeaseAsync(string titleNumber, CancellationToken cancellationToken = default);
}
