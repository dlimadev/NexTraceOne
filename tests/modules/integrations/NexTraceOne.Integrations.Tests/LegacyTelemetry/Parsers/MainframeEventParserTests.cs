using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Parsers;

public sealed class MainframeEventParserTests
{
    private readonly MainframeEventParser _parser = new();

    [Fact]
    public void Parse_OperationalEvent_ReturnsCorrectType()
    {
        var request = new MainframeEventRequest(
            Provider: "Z_CDP", CorrelationId: null, SourceType: "operational",
            SystemName: "SYS1", LparName: "LPAR01",
            EventType: null, Message: "System operational event",
            Severity: "info", EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("mainframe_operational", result.EventType);
        Assert.Equal(LegacyEventSourceType.Mainframe, result.SourceType);
        Assert.Equal(LegacySeverity.Info, result.Severity);
        Assert.Equal("SYS1", result.SystemName);
    }

    [Fact]
    public void Parse_CicsStatEvent_MapsToCicsSourceType()
    {
        var request = new MainframeEventRequest(
            Provider: "OMEGAMON", CorrelationId: null, SourceType: "cics_stat",
            SystemName: "SYS1", LparName: "LPAR01",
            EventType: null, Message: "CICS statistics collected",
            Severity: "info", EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("cics_statistics", result.EventType);
        Assert.Equal(LegacyEventSourceType.Cics, result.SourceType);
    }

    [Fact]
    public void Parse_ImsStatEvent_MapsToImsSourceType()
    {
        var request = new MainframeEventRequest(
            Provider: "OMEGAMON", CorrelationId: null, SourceType: "ims_stat",
            SystemName: "SYS2", LparName: "LPAR02",
            EventType: null, Message: "IMS statistics collected",
            Severity: "warning", EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("ims_statistics", result.EventType);
        Assert.Equal(LegacyEventSourceType.Ims, result.SourceType);
        Assert.Equal(LegacySeverity.Warning, result.Severity);
    }

    [Fact]
    public void Parse_SmfSourceType_ReturnsMainframeSmf()
    {
        var request = new MainframeEventRequest(
            Provider: "SMF", CorrelationId: null, SourceType: "smf",
            SystemName: "SYS3", LparName: null,
            EventType: null, Message: "SMF record type 30",
            Severity: null, EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("mainframe_smf", result.EventType);
        Assert.Equal(LegacyEventSourceType.Mainframe, result.SourceType);
    }

    [Fact]
    public void Parse_ExplicitEventType_OverridesDefault()
    {
        var request = new MainframeEventRequest(
            Provider: "Z_CDP", CorrelationId: null, SourceType: "operational",
            SystemName: "SYS1", LparName: null,
            EventType: "custom_event_type", Message: "Custom event",
            Severity: "error", EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("custom_event_type", result.EventType);
        Assert.Equal(LegacySeverity.Error, result.Severity);
    }

    [Fact]
    public void Parse_SeverityNormalization()
    {
        var request = new MainframeEventRequest(
            Provider: null, CorrelationId: null, SourceType: null,
            SystemName: null, LparName: null,
            EventType: null, Message: null,
            Severity: "CRITICAL", EventTimestamp: null, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal(LegacySeverity.Critical, result.Severity);
    }

    [Fact]
    public void Parse_WithMetadata_IncludesAllAttributes()
    {
        var metadata = new Dictionary<string, string> { { "subsystem", "DB2" }, { "version", "12.1" } };
        var request = new MainframeEventRequest(
            Provider: "OMEGAMON", CorrelationId: "c-001", SourceType: "operational",
            SystemName: "SYS1", LparName: "LP1",
            EventType: "db2_event", Message: "DB2 subsystem event",
            Severity: "info", EventTimestamp: DateTimeOffset.UtcNow, Metadata: metadata);

        var result = _parser.Parse(request);

        Assert.Contains("subsystem", result.Attributes.Keys);
        Assert.Contains("version", result.Attributes.Keys);
        Assert.Contains("provider", result.Attributes.Keys);
        Assert.Contains("correlation_id", result.Attributes.Keys);
    }
}
