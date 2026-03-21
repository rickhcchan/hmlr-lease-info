namespace HmlrLeaseInfo.Core.Parsing;

/// <summary>
/// Fixed-width column layout for HMLR schedule entries.
/// Tabular lines are 73 characters wide with 4 columns at fixed positions.
/// </summary>
public static class ColumnDefinitions
{
    /// <summary>Standard line width for tabular data (73 characters).</summary>
    public const int FullWidth = 73;

    // Column boundaries: [start, end) — end is exclusive
    private static readonly (int Start, int End)[] Columns =
    [
        (0, 16),   // Col1: Registration Date & Plan Ref
        (16, 46),  // Col2: Property Description
        (46, 62),  // Col3: Date of Lease & Term
        (62, 73),  // Col4: Lessee's Title
    ];

    /// <summary>
    /// Splits a line at the fixed column positions, trimming each field.
    /// </summary>
    public static string[] SplitLine(string line)
    {
        var result = new string[4];
        for (int i = 0; i < 4; i++)
        {
            var (start, end) = Columns[i];
            if (start < line.Length)
            {
                int length = Math.Min(end, line.Length) - start;
                result[i] = line.Substring(start, length).Trim();
            }
            else
            {
                result[i] = string.Empty;
            }
        }
        return result;
    }

    /// <summary>
    /// Pads a short line to 73 characters. Lines with trailing whitespace are
    /// padded left (content shifts right); lines without are padded right.
    /// </summary>
    public static string PadToFullWidth(string line)
    {
        if (line.Length >= FullWidth)
            return line;

        bool hasTrailingSpace = line.Length > 0 && line[^1] == ' ';

        if (hasTrailingSpace)
            return line.PadLeft(FullWidth);
        else
            return line.PadRight(FullWidth);
    }
}
