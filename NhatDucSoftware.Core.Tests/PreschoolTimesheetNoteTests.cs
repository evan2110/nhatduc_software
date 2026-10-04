using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class PreschoolTimesheetNoteTests
{
    [Fact]
    public void IsValid_AcceptsExample()
    {
        Assert.True(PreschoolTimesheetNote.IsValid("Lớn 1: 30, 2(An,Mỹ)", out _));
    }

    [Fact]
    public void IsValid_AcceptsZeroAbsentWithoutNames()
    {
        Assert.True(PreschoolTimesheetNote.IsValid("Bé 2: 20, 0()", out _));
    }

    [Fact]
    public void IsValid_RejectsMissingNamesWhenSomeoneIsAbsent()
    {
        Assert.False(PreschoolTimesheetNote.IsValid("Lớn 1: 30, 2()", out var error));
        Assert.Contains("tên", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsValid_RejectsEmptyAndWrongShape()
    {
        Assert.False(PreschoolTimesheetNote.IsValid("   ", out _));
        Assert.False(PreschoolTimesheetNote.IsValid("đi dạy bù", out _));
    }
}
