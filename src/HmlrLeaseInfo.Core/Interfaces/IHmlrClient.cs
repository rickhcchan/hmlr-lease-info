using HmlrLeaseInfo.Core.Models;

namespace HmlrLeaseInfo.Core.Interfaces;

/// <summary>
/// HTTP client for the HMLR mock API.
/// </summary>
public interface IHmlrClient
{
    /// <summary>
    /// Fetches all raw schedule entries from the HMLR API.
    /// </summary>
    Task<IEnumerable<RawNoticeOfLease>> GetSchedulesAsync(CancellationToken cancellationToken = default);
}
