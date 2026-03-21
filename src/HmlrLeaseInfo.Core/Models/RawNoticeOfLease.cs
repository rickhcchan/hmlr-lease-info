namespace HmlrLeaseInfo.Core.Models;

/// <summary>
/// Raw Schedule of Notice of Lease entry as returned by the HMLR mock API.
/// </summary>
public record RawNoticeOfLease(
    string EntryNumber,
    string EntryDate,
    string EntryType,
    IReadOnlyList<string> EntryText
);
