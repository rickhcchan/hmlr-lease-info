namespace HmlrLeaseInfo.Core.Parsing;

using System.Text.RegularExpressions;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Models;

/// <summary>
/// Parses raw HMLR schedule entries using a 3-step algorithm:
/// 1. Extract trailing NOTE lines from the entry text.
/// 2. Process tabular lines using fixed-width column rules (A/B/C/D).
/// 3. Assemble column fragments into structured fields.
/// </summary>
public class LeaseParser : ILeaseParser
{
    private static readonly Regex NotePattern = new(@"^NOTE(\s\d+)?\s*:", RegexOptions.Compiled);

    public ParsedNoticeOfLease Parse(RawNoticeOfLease raw)
    {
        var (tabularLines, notes) = ExtractNotes(raw.EntryText);
        var columns = ProcessTabularLines(tabularLines);
        return AssembleFields(raw, columns, notes);
    }

    private static (List<string> Tabular, List<string> Notes) ExtractNotes(IReadOnlyList<string> lines)
    {
        var notes = new List<string>();
        int lastTabular = lines.Count - 1;

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (NotePattern.IsMatch(lines[i]))
            {
                notes.Add(lines[i]);
                lastTabular = i - 1;
            }
            else
            {
                break;
            }
        }

        notes.Reverse();
        return (lines.Take(lastTabular + 1).ToList(), notes);
    }

    private static List<string[]> ProcessTabularLines(List<string> lines)
    {
        var allColumns = new List<string[]>();
        int previousNonEmptyCount = 0;
        int previousStickyColumn = -1;

        foreach (var line in lines)
        {
            // Rule A: 73 chars — fixed-width split
            if (line.Length == ColumnDefinitions.FullWidth)
            {
                var cols = ColumnDefinitions.SplitLine(line);
                allColumns.Add(cols);
                UpdateStickyState(cols, out previousNonEmptyCount, out previousStickyColumn);
                continue;
            }

            // Rule B: Previous row had exactly 1 non-empty column — sticky append
            if (previousNonEmptyCount == 1 && previousStickyColumn >= 0)
            {
                var lastCols = allColumns[^1];
                lastCols[previousStickyColumn] += " " + line.Trim();
                // previousNonEmptyCount stays 1, previousStickyColumn stays same
                continue;
            }

            // Rule C & D: Pad and split
            var padded = ColumnDefinitions.PadToFullWidth(line);
            var splitCols = ColumnDefinitions.SplitLine(padded);
            allColumns.Add(splitCols);
            UpdateStickyState(splitCols, out previousNonEmptyCount, out previousStickyColumn);
        }

        return allColumns;
    }

    private static void UpdateStickyState(string[] cols, out int nonEmptyCount, out int stickyColumn)
    {
        nonEmptyCount = 0;
        stickyColumn = -1;

        for (int i = 0; i < 4; i++)
        {
            if (!string.IsNullOrEmpty(cols[i]))
            {
                nonEmptyCount++;
                if (stickyColumn < 0)
                    stickyColumn = i;
            }
        }

        // Only keep sticky column if exactly 1 non-empty
        if (nonEmptyCount != 1)
            stickyColumn = -1;
    }

    private static ParsedNoticeOfLease AssembleFields(
        RawNoticeOfLease raw,
        List<string[]> allColumns,
        List<string> notes)
    {
        var fields = new string[4];
        for (int col = 0; col < 4; col++)
        {
            fields[col] = string.Join(" ", allColumns
                .Select(row => row[col])
                .Where(s => !string.IsNullOrEmpty(s)));
        }

        if (!int.TryParse(raw.EntryNumber, out int entryNumber))
            throw new FormatException($"Invalid entry number: '{raw.EntryNumber}'.");
        DateOnly? entryDate = string.IsNullOrWhiteSpace(raw.EntryDate)
            ? null
            : DateOnly.Parse(raw.EntryDate, System.Globalization.CultureInfo.InvariantCulture);

        return new ParsedNoticeOfLease(
            EntryNumber: entryNumber,
            EntryDate: entryDate,
            RegistrationDateAndPlanRef: fields[0],
            PropertyDescription: fields[1],
            DateOfLeaseAndTerm: fields[2],
            LesseesTitle: fields[3],
            Notes: notes
        );
    }
}
