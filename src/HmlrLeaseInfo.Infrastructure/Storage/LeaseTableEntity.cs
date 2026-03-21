namespace HmlrLeaseInfo.Infrastructure.Storage;

using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using HmlrLeaseInfo.Core.Models;

/// <summary>
/// Azure Table Storage entity for parsed lease entries.
/// Maps to/from ParsedNoticeOfLease.
/// </summary>
public class LeaseTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "lease";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public int EntryNumber { get; set; }
    public string? EntryDate { get; set; }
    public string RegistrationDateAndPlanRef { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string DateOfLeaseAndTerm { get; set; } = string.Empty;
    public string LesseesTitle { get; set; } = string.Empty;
    public string NotesJson { get; set; } = "[]";

    public static LeaseTableEntity FromDomain(ParsedNoticeOfLease parsed)
    {
        return new LeaseTableEntity
        {
            RowKey = parsed.LesseesTitle,
            EntryNumber = parsed.EntryNumber,
            EntryDate = parsed.EntryDate?.ToString("O"),
            RegistrationDateAndPlanRef = parsed.RegistrationDateAndPlanRef,
            PropertyDescription = parsed.PropertyDescription,
            DateOfLeaseAndTerm = parsed.DateOfLeaseAndTerm,
            LesseesTitle = parsed.LesseesTitle,
            NotesJson = JsonSerializer.Serialize(parsed.Notes)
        };
    }

    public ParsedNoticeOfLease ToDomain()
    {
        DateOnly? entryDate = EntryDate is not null
            ? DateOnly.Parse(EntryDate)
            : null;

        var notes = JsonSerializer.Deserialize<List<string>>(NotesJson) ?? [];

        return new ParsedNoticeOfLease(
            EntryNumber: EntryNumber,
            EntryDate: entryDate,
            RegistrationDateAndPlanRef: RegistrationDateAndPlanRef,
            PropertyDescription: PropertyDescription,
            DateOfLeaseAndTerm: DateOfLeaseAndTerm,
            LesseesTitle: LesseesTitle,
            Notes: notes
        );
    }
}
