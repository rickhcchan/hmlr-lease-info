namespace HmlrLeaseInfo.Core.Models;

/// <summary>
/// Raw Schedule of Notice of Lease entry as returned by the HMLR mock API.
/// </summary>
public record RawNoticeOfLease(
    /// <summary>Entry number within the schedule.</summary>
    string EntryNumber,
    /// <summary>Date the entry was added.</summary>
    string EntryDate,
    /// <summary>Type of the entry (e.g. "Schedule of Notices of Leases").</summary>
    string EntryType,
    /// <summary>Fixed-width columnar text lines containing lease details.</summary>
    IReadOnlyList<string> EntryText
);
