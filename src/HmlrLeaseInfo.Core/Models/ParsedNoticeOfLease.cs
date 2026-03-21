namespace HmlrLeaseInfo.Core.Models;

/// <summary>
/// Parsed lease entry matching the HMLR mock API's /results schema.
/// </summary>
public record ParsedNoticeOfLease(
    /// <summary>The entry number.</summary>
    int EntryNumber,
    /// <summary>Date the entry was added.</summary>
    DateOnly? EntryDate,
    /// <summary>Registration date and where it is referred to on the title plan.</summary>
    string RegistrationDateAndPlanRef,
    /// <summary>A brief description of the property.</summary>
    string PropertyDescription,
    /// <summary>Date the lease was created and how long it will live for.</summary>
    string DateOfLeaseAndTerm,
    /// <summary>Title number of the lessee.</summary>
    string LesseesTitle,
    /// <summary>All appended notes to the entry.</summary>
    IReadOnlyList<string> Notes
);
