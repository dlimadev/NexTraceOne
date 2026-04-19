using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ResolveIncident;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Tests.Incidents.Infrastructure;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para o handler ResolveIncident.
/// Verifica resolução de incidente, idempotência, incidente não encontrado e validação de entrada.
/// </summary>
public sealed class ResolveIncidentTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 21, 0, 0, TimeSpan.Zero);

    private readonly InMemoryIncidentStore _store = new();

    private IDateTimeProvider CreateClock() =>
        CreateClock(FixedNow);

    private static IDateTimeProvider CreateClock(DateTimeOffset value)
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(value);
        return clock;
    }

    // ── Resolve Open Incident ────────────────────────────────────────────

    [Fact]
    public async Task ResolveIncident_KnownOpenIncident_ShouldReturnResolved()
    {
        var handler = new ResolveIncident.Handler(_store, CreateClock());
        var command = new ResolveIncident.Command(
            IncidentId: "a1b2c3d4-0001-0000-0000-000000000001",
            ResolvedAtUtc: FixedNow,
            ResolutionNote: "Root cause identified and fixed.");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(IncidentStatus.Resolved);
        result.Value.ResolvedAt.Should().Be(FixedNow);
        result.Value.ResolutionNote.Should().Be("Root cause identified and fixed.");
        result.Value.IncidentId.Should().Be("a1b2c3d4-0001-0000-0000-000000000001");
    }

    [Fact]
    public async Task ResolveIncident_WhenResolvedAtOmitted_ShouldUseClockUtcNow()
    {
        var clock = CreateClock(FixedNow);
        var handler = new ResolveIncident.Handler(_store, clock);
        var command = new ResolveIncident.Command(
            IncidentId: "a1b2c3d4-0001-0000-0000-000000000001",
            ResolvedAtUtc: null,
            ResolutionNote: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResolvedAt.Should().Be(FixedNow);
    }

    // ── Not Found ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveIncident_UnknownIncident_ShouldReturnNotFound()
    {
        var handler = new ResolveIncident.Handler(_store, CreateClock());
        var command = new ResolveIncident.Command(
            IncidentId: "00000000-0000-0000-0000-000000000000",
            ResolvedAtUtc: null,
            ResolutionNote: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── Idempotency ──────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveIncident_AlreadyResolved_ShouldBeIdempotent()
    {
        var handler = new ResolveIncident.Handler(_store, CreateClock());
        var incidentId = "a1b2c3d4-0001-0000-0000-000000000001";

        // First resolve
        var first = await handler.Handle(
            new ResolveIncident.Command(incidentId, FixedNow, null),
            CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        // Second resolve — should also succeed (idempotent at store level)
        var second = await handler.Handle(
            new ResolveIncident.Command(incidentId, FixedNow.AddMinutes(5), "Second attempt"),
            CancellationToken.None);
        second.IsSuccess.Should().BeTrue();
        // Status remains Resolved
        second.Value.Status.Should().Be(IncidentStatus.Resolved);
    }

    // ── Validator ────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_WhenIncidentIdEmpty_ShouldFail()
    {
        var validator = new ResolveIncident.Validator();
        var result = await validator.ValidateAsync(
            new ResolveIncident.Command("", null, null),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "IncidentId");
    }

    [Fact]
    public async Task Validator_WhenResolutionNoteTooLong_ShouldFail()
    {
        var validator = new ResolveIncident.Validator();
        var result = await validator.ValidateAsync(
            new ResolveIncident.Command("incident-1", null, new string('x', 2001)),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ResolutionNote");
    }

    [Fact]
    public async Task Validator_WhenValid_ShouldPass()
    {
        var validator = new ResolveIncident.Validator();
        var result = await validator.ValidateAsync(
            new ResolveIncident.Command("incident-1", FixedNow, "Service restored after rollback."),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_WhenResolutionNoteNull_ShouldPass()
    {
        var validator = new ResolveIncident.Validator();
        var result = await validator.ValidateAsync(
            new ResolveIncident.Command("incident-1", null, null),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
