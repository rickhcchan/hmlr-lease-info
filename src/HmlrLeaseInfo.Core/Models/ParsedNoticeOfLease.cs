namespace HmlrLeaseInfo.Core.Models;

/// <summary>
/// Parsed lease entry matching the HMLR mock API's /results schema.
/// </summary>
public record ParsedNoticeOfLease(
    int EntryNumber,
    DateOnly? EntryDate,
    string RegistrationDateAndPlanRef,
    string PropertyDescription,
    string DateOfLeaseAndTerm,
    string LesseesTitle,
    IReadOnlyList<string> Notes
);
