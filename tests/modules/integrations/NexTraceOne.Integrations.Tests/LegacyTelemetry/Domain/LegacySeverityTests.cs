using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Domain;

public sealed class LegacySeverityTests
{
    [Theory]
    [InlineData("info", "info")]
    [InlineData("Info", "info")]
    [InlineData("INFO", "info")]
    [InlineData("information", "info")]
    [InlineData("informational", "info")]
    [InlineData("warn", "warning")]
    [InlineData("warning", "warning")]
    [InlineData("WARNING", "warning")]
    [InlineData("error", "error")]
    [InlineData("err", "error")]
    [InlineData("ERROR", "error")]
    [InlineData("critical", "critical")]
    [InlineData("crit", "critical")]
    [InlineData("fatal", "critical")]
    [InlineData("FATAL", "critical")]
    [InlineData("unknown", "info")]
    [InlineData(null, "info")]
    [InlineData("", "info")]
    public void Normalize_ReturnsExpectedSeverity(string? input, string expected)
    {
        var result = LegacySeverity.Normalize(input);
        Assert.Equal(expected, result);
    }
}
