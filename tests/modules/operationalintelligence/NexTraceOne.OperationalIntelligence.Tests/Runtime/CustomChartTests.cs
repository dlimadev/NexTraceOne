using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime;

public sealed class CustomChartTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnChart()
    {
        var now = DateTimeOffset.UtcNow;
        var chart = CustomChart.Create("tenant1", "user1", "My Chart", ChartType.Line,
            """{"source":"changes"}""", "last_24h", null, now);

        Assert.Equal("My Chart", chart.Name);
        Assert.Equal(ChartType.Line, chart.ChartType);
        Assert.Equal("last_24h", chart.TimeRange);
        Assert.False(chart.IsShared);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            CustomChart.Create("tenant1", "user1", "", ChartType.Bar, """{"source":"incidents"}""", "last_7d", null, now));
    }

    [Fact]
    public void UpdateDetails_ShouldChangeNameAndType()
    {
        var now = DateTimeOffset.UtcNow;
        var chart = CustomChart.Create("tenant1", "user1", "Old Name", ChartType.Line, """{"source":"changes"}""", "last_24h", null, now);
        chart.UpdateDetails("New Name", ChartType.Bar, """{"source":"incidents"}""", "last_7d", null, now.AddMinutes(1));
        Assert.Equal("New Name", chart.Name);
        Assert.Equal(ChartType.Bar, chart.ChartType);
    }

    [Fact]
    public void SetShared_ShouldToggleFlag()
    {
        var now = DateTimeOffset.UtcNow;
        var chart = CustomChart.Create("tenant1", "user1", "Chart", ChartType.Pie, """{"source":"slos"}""", "last_30d", null, now);
        Assert.False(chart.IsShared);
        chart.SetShared(true, now.AddMinutes(1));
        Assert.True(chart.IsShared);
    }

    [Fact]
    public void Create_WithEmptyMetricQuery_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            CustomChart.Create("tenant1", "user1", "Chart", ChartType.Line, "", "last_24h", null, now));
    }
}
