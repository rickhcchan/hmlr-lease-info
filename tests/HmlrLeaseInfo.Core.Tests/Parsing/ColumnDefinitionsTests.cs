namespace HmlrLeaseInfo.Core.Tests.Parsing;

using FluentAssertions;
using HmlrLeaseInfo.Core.Parsing;

public class ColumnDefinitionsTests
{
    [Fact]
    public void SplitLine_73CharLine_SplitsAtCorrectPositions()
    {
        var line = "09.07.2009      Endeavour House, 47 Cuba      06.07.2009      EGL557357  ";
        var cols = ColumnDefinitions.SplitLine(line);
        cols[0].Should().Be("09.07.2009");
        cols[1].Should().Be("Endeavour House, 47 Cuba");
        cols[2].Should().Be("06.07.2009");
        cols[3].Should().Be("EGL557357");
    }

    [Fact]
    public void SplitLine_73CharLine_EmptyColumns_ReturnsEmptyStrings()
    {
        var line = "(Part of) in                                                             ";
        var cols = ColumnDefinitions.SplitLine(line);
        cols[0].Should().Be("(Part of) in");
        cols[1].Should().BeEmpty();
        cols[2].Should().BeEmpty();
        cols[3].Should().BeEmpty();
    }

    [Fact]
    public void PadToFullWidth_ShortLineWithTrailingSpace_PadsLeft()
    {
        var line = "including 19               "; // trailing spaces
        var padded = ColumnDefinitions.PadToFullWidth(line);
        padded.Length.Should().Be(73);
        // After pad-left, content should be in Col3 position (46-61)
        var cols = ColumnDefinitions.SplitLine(padded);
        cols[2].Should().Be("including 19");
    }

    [Fact]
    public void PadToFullWidth_ShortLineNoTrailingSpace_PadsRight()
    {
        var line = "blue (part of)"; // no trailing space
        var padded = ColumnDefinitions.PadToFullWidth(line);
        padded.Length.Should().Be(73);
        // After pad-right, content should be in Col1 position (0-15)
        var cols = ColumnDefinitions.SplitLine(padded);
        cols[0].Should().Be("blue (part of)");
    }

    [Fact]
    public void PadToFullWidth_AlreadyFullWidth_ReturnsUnchanged()
    {
        var line = "09.07.2009      Endeavour House, 47 Cuba      06.07.2009      EGL557357  ";
        var padded = ColumnDefinitions.PadToFullWidth(line);
        padded.Should().Be(line);
    }
}
