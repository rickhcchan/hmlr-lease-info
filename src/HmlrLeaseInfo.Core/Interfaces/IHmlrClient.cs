namespace HmlrLeaseInfo.Core.Interfaces;

using HmlrLeaseInfo.Core.Models;

/// <summary>
/// HTTP client for the HMLR mock API.
/// </summary>
public interface IHmlrClient
{
    /// <summary>
    /// Fetches all raw schedule entries from GET /schedules.
    /// </summary>
    Task<IEnumerable<RawNoticeOfLease>> GetSchedulesAsync(CancellationToken cancellationToken = default);
}
