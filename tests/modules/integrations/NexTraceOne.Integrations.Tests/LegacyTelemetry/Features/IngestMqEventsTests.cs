using System.Linq;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestMqEvents;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Features;

public sealed class IngestMqEventsValidatorTests
{
    private readonly IngestMqEvents.Validator _validator = new();

    [Fact]
    public void Validate_EmptyEvents_Fails()
    {
        var command = new IngestMqEvents.Command(new());
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TooManyEvents_Fails()
    {
        var events = Enumerable.Range(0, 1001)
            .Select(_ => new MqEventRequest(
                null, null, null, null, null, null, null, null, null, null, null, null, null))
            .ToList();

        var command = new IngestMqEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ValidSingleEvent_Passes()
    {
        var events = new List<MqEventRequest>
        {
            new(null, null, "QMGR", "QUEUE", null, null, null, null, null, null, null, null, null)
        };

        var command = new IngestMqEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }
}
