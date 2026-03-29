using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Parsers;

public sealed class SmfRecordParserTests
{
    private readonly SmfRecordParser _parser = new();

    [Fact]
    public void Parse_ValidJson_ReturnsNormalizedEvent()
    {
        var json = """
        {
            "record_type": "30",
            "system_name": "SYS1",
            "lpar_name": "LPAR01",
            "timestamp": "2026-03-29T10:30:00Z",
            "severity": "info",
            "message": "Program execution completed",
            "program_name": "PGMTEST"
        }
        """;

        var result = _parser.Parse(json);

        Assert.Equal("smf_30", result.EventType);
        Assert.Equal(LegacyEventSourceType.Mainframe, result.SourceType);
        Assert.Equal("SYS1", result.SystemName);
        Assert.Equal("LPAR01", result.LparName);
        Assert.Equal(LegacySeverity.Info, result.Severity);
        Assert.Contains("program_name", result.Attributes.Keys);
    }

    [Fact]
    public void Parse_AlternativeFieldNames_ReturnsNormalizedEvent()
    {
        var json = """
        {
            "smf_type": "72",
            "system": "SYS2",
            "lpar": "LP2",
            "timestamp": "2026-03-29T11:00:00Z",
            "description": "Workload stats"
        }
        """;

        var result = _parser.Parse(json);

        Assert.Equal("smf_72", result.EventType);
        Assert.Equal("SYS2", result.SystemName);
        Assert.Equal("LP2", result.LparName);
        Assert.Equal("Workload stats", result.Message);
    }

    [Fact]
    public void Parse_ExtraFields_ExtractedAsAttributes()
    {
        var json = """
        {
            "record_type": "30",
            "timestamp": "2026-03-29T12:00:00Z",
            "cpu_time": 15.3,
            "elapsed_time": 120.5,
            "step_count": 5
        }
        """;

        var result = _parser.Parse(json);

        Assert.Contains("cpu_time", result.Attributes.Keys);
        Assert.Contains("elapsed_time", result.Attributes.Keys);
        Assert.Contains("step_count", result.Attributes.Keys);
    }

    [Fact]
    public void Parse_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_NoTimestamp_UsesUtcNow()
    {
        var json = """{"record_type": "30"}""";
        var before = DateTimeOffset.UtcNow;

        var result = _parser.Parse(json);

        Assert.True(result.Timestamp >= before.AddSeconds(-1));
    }
}
