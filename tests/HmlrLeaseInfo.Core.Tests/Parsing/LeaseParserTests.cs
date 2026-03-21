namespace HmlrLeaseInfo.Core.Tests.Parsing;

using FluentAssertions;
using HmlrLeaseInfo.Core.Models;
using HmlrLeaseInfo.Core.Parsing;

public class LeaseParserTests
{
    private readonly LeaseParser _parser = new();

    // Golden tests — all 5 entries must match mock API's /results exactly

    [Fact]
    public void Parse_Entry1_PadRight_MatchesExpected()
    {
        var result = _parser.Parse(TestFixtures.RawEntry1);
        result.Should().BeEquivalentTo(TestFixtures.ExpectedParsed1);
    }

    [Fact]
    public void Parse_Entry2_StickyColumnAndPadLeft_MatchesExpected()
    {
        var result = _parser.Parse(TestFixtures.RawEntry2);
        result.Should().BeEquivalentTo(TestFixtures.ExpectedParsed2);
    }

    [Fact]
    public void Parse_Entry3_StickyColumnMultipleLines_MatchesExpected()
    {
        var result = _parser.Parse(TestFixtures.RawEntry3);
        result.Should().BeEquivalentTo(TestFixtures.ExpectedParsed3);
    }

    [Fact]
    public void Parse_Entry4_AllFullWidthWithNotes_MatchesExpected()
    {
        var result = _parser.Parse(TestFixtures.RawEntry4);
        result.Should().BeEquivalentTo(TestFixtures.ExpectedParsed4);
    }

    [Fact]
    public void Parse_Entry5_AllFullWidthWithSingleNote_MatchesExpected()
    {
        var result = _parser.Parse(TestFixtures.RawEntry5);
        result.Should().BeEquivalentTo(TestFixtures.ExpectedParsed5);
    }

    // Note extraction edge cases

    [Fact]
    public void Parse_EntryWithNoNotes_NotesListIsEmpty()
    {
        var result = _parser.Parse(TestFixtures.RawEntry1);
        result.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EntryWithMultipleNotes_PreservesOrder()
    {
        var result = _parser.Parse(TestFixtures.RawEntry4);
        result.Notes.Should().HaveCount(3);
        result.Notes[0].Should().StartWith("NOTE 1:");
        result.Notes[1].Should().StartWith("NOTE 2:");
        result.Notes[2].Should().StartWith("NOTE 3:");
    }

    [Fact]
    public void Parse_EntryWithSingleNote_ExtractsCorrectly()
    {
        var result = _parser.Parse(TestFixtures.RawEntry5);
        result.Notes.Should().HaveCount(1);
        result.Notes[0].Should().StartWith("NOTE:");
    }

    // Defensive edge cases

    [Fact]
    public void Parse_InvalidEntryNumber_ThrowsFormatException()
    {
        var raw = new RawNoticeOfLease("abc", "", "Schedule of Notices of Leases", new List<string> { "some text" });
        var act = () => _parser.Parse(raw);
        act.Should().Throw<FormatException>().WithMessage("*abc*");
    }

    [Fact]
    public void Parse_EmptyEntryText_ReturnsEmptyFields()
    {
        var raw = new RawNoticeOfLease("99", "", "Schedule of Notices of Leases", new List<string>());
        var result = _parser.Parse(raw);
        result.RegistrationDateAndPlanRef.Should().BeEmpty();
        result.PropertyDescription.Should().BeEmpty();
        result.DateOfLeaseAndTerm.Should().BeEmpty();
        result.LesseesTitle.Should().BeEmpty();
        result.Notes.Should().BeEmpty();
    }
}
