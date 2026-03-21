namespace HmlrLeaseInfo.Core.Interfaces;

using HmlrLeaseInfo.Core.Models;

/// <summary>
/// Persistence layer for parsed lease entries.
/// </summary>
public interface ILeaseRepository
{
    /// <summary>
    /// Retrieves a parsed lease entry by the lessee's title number.
    /// </summary>
    Task<ParsedNoticeOfLease?> GetAsync(string lesseesTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a collection of parsed lease entries.
    /// </summary>
    Task UpsertParsedAsync(IEnumerable<ParsedNoticeOfLease> entries, CancellationToken cancellationToken = default);
}
