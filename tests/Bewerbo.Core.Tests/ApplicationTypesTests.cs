namespace Bewerbo.Core.Tests;

public class ApplicationTypesTests
{
    [Fact]
    public void Groups_types_ignoring_case_and_leaves_out_empty_ones()
    {
        var groups = ApplicationTypes.Group(
        [
            TestData.Application(type: "Werkstudent"),
            TestData.Application(type: " werkstudent"),
            TestData.Application(type: "Job"),
            TestData.Application(type: ""),
        ]);

        Assert.Equal(["Werkstudent", "Job"], groups.Select(g => g.Name));
        Assert.Equal([2, 1], groups.Select(g => g.Count));
    }

    [Fact]
    public void Colour_slots_are_stable_and_alphabetical()
    {
        string[] types = ["Job", "Ausbildung", "job", "Internship", "Werkstudent", "Minijob"];

        Assert.Equal(0, ApplicationTypes.ColorIndex("Ausbildung", types));
        Assert.Equal(1, ApplicationTypes.ColorIndex("Internship", types));
        Assert.Equal(2, ApplicationTypes.ColorIndex("JOB", types));
        Assert.Equal(3, ApplicationTypes.ColorIndex("Minijob", types));
        Assert.Equal(0, ApplicationTypes.ColorIndex("Werkstudent", types));
        Assert.Equal(0, ApplicationTypes.ColorIndex("", types));
    }
}
