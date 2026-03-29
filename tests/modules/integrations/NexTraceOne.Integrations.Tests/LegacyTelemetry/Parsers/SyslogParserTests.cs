using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Parsers;

public sealed class SyslogParserTests
{
    private readonly SyslogParser _parser = new();

    [Fact]
    public void Parse_ValidLine_ReturnsNormalizedEvent()
    {
        var line = "2026-03-29T10:00:00Z SYS1 IEF285I BATCH1 ENDED. NAME-PAYROLL01";

        var result = _parser.Parse(line);

        Assert.Equal("mainframe_syslog", result.EventType);
        Assert.Equal(LegacyEventSourceType.Mainframe, result.SourceType);
        Assert.Equal("SYS1", result.SystemName);
        Assert.Contains("IEF285I", result.Message!);
    }

    [Fact]
    public void Parse_AbendMessage_ReturnsCriticalSeverity()
    {
        var line = "2026-03-29T10:05:00Z SYS2 ABEND S0C7 in program PGMFAIL";

        var result = _parser.Parse(line);

        Assert.Equal(LegacySeverity.Critical, result.Severity);
    }

    [Fact]
    public void Parse_ErrorMessage_ReturnsErrorSeverity()
    {
        var line = "2026-03-29T10:10:00Z SYS3 ERROR processing request for queue Q.APP";

        var result = _parser.Parse(line);

        Assert.Equal(LegacySeverity.Error, result.Severity);
    }

    [Fact]
    public void Parse_IefMessage_ReturnsWarningSeverity()
    {
        var line = "2026-03-29T10:15:00Z SYS1 IEF450I JOBX - ABEND=S222 U0000 REASON=00000000";

        var result = _parser.Parse(line);

        Assert.Equal(LegacySeverity.Critical, result.Severity); // Contains ABEND
    }

    [Fact]
    public void Parse_NonMatchingLine_FallsBackGracefully()
    {
        // Even a plain line matches the regex: timestamp="Just", system="a", message="plain ..."
        var line = "NoSpacesHereSoNoMatch";

        var result = _parser.Parse(line);

        Assert.Equal("mainframe_syslog", result.EventType);
        Assert.Equal("NoSpacesHereSoNoMatch", result.Message);
        Assert.Null(result.SystemName);
    }

    [Fact]
    public void Parse_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_LongLine_TruncatesRawAttribute()
    {
        var line = "2026-03-29T10:00:00Z SYS1 " + new string('A', 600);

        var result = _parser.Parse(line);

        Assert.True(result.Attributes["raw_line"].Length <= 500);
    }
}
