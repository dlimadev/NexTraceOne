using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

using UpdateRunbookFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateRunbook.UpdateRunbook;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para UpdateRunbook — atualiza runbook operacional existente.
/// Valida happy path, runbook não encontrado, serialização de passos e validação.
/// </summary>
public sealed class UpdateRunbookTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 17, 0, 0, TimeSpan.Zero);

    private readonly IRunbookRepository _repo = Substitute.For<IRunbookRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private UpdateRunbookFeature.Handler CreateHandler()
    {
        _clock.UtcNow.Returns(FixedNow);
        return new(_repo, _clock);
    }

    private static RunbookRecord MakeRunbook(Guid? id = null) =>
        RunbookRecord.Create(
            id: RunbookRecordId.From(id ?? Guid.NewGuid()),
            title: "Original Title",
            description: "Original description of the runbook.",
            linkedService: "checkout-api",
            linkedIncidentType: "DatabaseFailure",
            stepsJson: null,
            prerequisitesJson: null,
            postNotes: null,
            maintainedBy: "ops@corp.com",
            publishedAt: FixedNow.AddDays(-30));

    private static UpdateRunbookFeature.Command MakeCommand(Guid runbookId) =>
        new(
            RunbookId: runbookId,
            Title: "Updated Title",
            Description: "Updated description that is more detailed.",
            LinkedService: "checkout-api",
            LinkedIncidentType: "DatabaseFailure",
            Steps:
            [
                new UpdateRunbookFeature.UpdateRunbookStepDto(1, "Check DB connectivity", "Use ping or telnet.", false),
                new UpdateRunbookFeature.UpdateRunbookStepDto(2, "Restart service", null, true)
            ],
            Prerequisites: ["Verify backup is available"],
            PostNotes: "Update ticket after resolution.",
            MaintainedBy: "sre@corp.com");

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_RunbookFound_UpdatesAndReturnsResponse()
    {
        var runbookId = Guid.NewGuid();
        var runbook = MakeRunbook(runbookId);

        _repo.GetByIdForUpdateAsync(runbookId, Arg.Any<CancellationToken>())
            .Returns(runbook);

        var result = await CreateHandler().Handle(MakeCommand(runbookId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RunbookId.Should().Be(runbookId);
        result.Value.UpdatedAt.Should().Be(FixedNow);
        await _repo.Received(1).UpdateAsync(runbook, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RunbookFound_FieldsAreUpdated()
    {
        var runbookId = Guid.NewGuid();
        var runbook = MakeRunbook(runbookId);

        _repo.GetByIdForUpdateAsync(runbookId, Arg.Any<CancellationToken>())
            .Returns(runbook);

        await CreateHandler().Handle(MakeCommand(runbookId), CancellationToken.None);

        runbook.Title.Should().Be("Updated Title");
        runbook.Description.Should().Be("Updated description that is more detailed.");
        runbook.MaintainedBy.Should().Be("sre@corp.com");
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_RunbookNotFound_ReturnsFailure()
    {
        var runbookId = Guid.NewGuid();

        _repo.GetByIdForUpdateAsync(runbookId, Arg.Any<CancellationToken>())
            .Returns((RunbookRecord?)null);

        var result = await CreateHandler().Handle(MakeCommand(runbookId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<RunbookRecord>(), Arg.Any<CancellationToken>());
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyRunbookId_ReturnsError()
    {
        var validator = new UpdateRunbookFeature.Validator();
        var result = validator.Validate(new UpdateRunbookFeature.Command(
            RunbookId: Guid.Empty,
            Title: "Title",
            Description: "Description",
            LinkedService: null,
            LinkedIncidentType: null,
            Steps: null,
            Prerequisites: null,
            PostNotes: null,
            MaintainedBy: "ops@corp.com"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "RunbookId");
    }

    [Fact]
    public void Validator_EmptyTitle_ReturnsError()
    {
        var validator = new UpdateRunbookFeature.Validator();
        var result = validator.Validate(new UpdateRunbookFeature.Command(
            RunbookId: Guid.NewGuid(),
            Title: "",
            Description: "Description",
            LinkedService: null,
            LinkedIncidentType: null,
            Steps: null,
            Prerequisites: null,
            PostNotes: null,
            MaintainedBy: "ops@corp.com"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var validator = new UpdateRunbookFeature.Validator();
        var result = validator.Validate(new UpdateRunbookFeature.Command(
            RunbookId: Guid.NewGuid(),
            Title: "Valid Title",
            Description: "Valid detailed description of the runbook.",
            LinkedService: "api-svc",
            LinkedIncidentType: null,
            Steps: [new UpdateRunbookFeature.UpdateRunbookStepDto(1, "Check logs", null, false)],
            Prerequisites: null,
            PostNotes: null,
            MaintainedBy: "team@corp.com"));

        result.IsValid.Should().BeTrue();
    }
}
