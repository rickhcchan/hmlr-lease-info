namespace HmlrLeaseInfo.Api.Services;

using HmlrLeaseInfo.Api.Interfaces;

/// <summary>
/// Orchestrates lease lookup with caching, sync triggering, and response selection.
/// </summary>
public class LeaseService : ILeaseService
{
    /// <inheritdoc />
    public Task<IResult> GetLeaseAsync(string titleNumber, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
