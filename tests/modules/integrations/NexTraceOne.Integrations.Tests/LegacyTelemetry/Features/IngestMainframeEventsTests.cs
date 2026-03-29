using System.Linq;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestMainframeEvents;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Features;

public sealed class IngestMainframeEventsValidatorTests
{
    private readonly IngestMainframeEvents.Validator _validator = new();

    [Fact]
    public void Validate_EmptyEvents_Fails()
    {
        var command = new IngestMainframeEvents.Command(new());
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TooManyEvents_Fails()
    {
        var events = Enumerable.Range(0, 1001)
            .Select(_ => new MainframeEventRequest(
                null, null, null, null, null, null, null, null, null, null))
            .ToList();

        var command = new IngestMainframeEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ValidSingleEvent_Passes()
    {
        var events = new List<MainframeEventRequest>
        {
            new("Z_CDP", null, "operational", "SYS1", "LPAR01", "operational_event", "Test message", "info", DateTimeOffset.UtcNow, null)
        };

        var command = new IngestMainframeEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LongMessage_Fails()
    {
        var events = new List<MainframeEventRequest>
        {
            new(null, null, null, null, null, null, new string('X', 10001), null, null, null)
        };

        var command = new IngestMainframeEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }
}
