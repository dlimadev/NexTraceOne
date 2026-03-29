using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Domain;

public sealed class LegacyEventSourceTypeTests
{
    [Fact]
    public void All_ContainsAllTypes()
    {
        Assert.Contains(LegacyEventSourceType.Batch, LegacyEventSourceType.All);
        Assert.Contains(LegacyEventSourceType.Mq, LegacyEventSourceType.All);
        Assert.Contains(LegacyEventSourceType.Cics, LegacyEventSourceType.All);
        Assert.Contains(LegacyEventSourceType.Ims, LegacyEventSourceType.All);
        Assert.Contains(LegacyEventSourceType.Mainframe, LegacyEventSourceType.All);
        Assert.Equal(5, LegacyEventSourceType.All.Length);
    }

    [Fact]
    public void Constants_HaveExpectedValues()
    {
        Assert.Equal("batch", LegacyEventSourceType.Batch);
        Assert.Equal("mq", LegacyEventSourceType.Mq);
        Assert.Equal("cics", LegacyEventSourceType.Cics);
        Assert.Equal("ims", LegacyEventSourceType.Ims);
        Assert.Equal("mainframe", LegacyEventSourceType.Mainframe);
    }
}
