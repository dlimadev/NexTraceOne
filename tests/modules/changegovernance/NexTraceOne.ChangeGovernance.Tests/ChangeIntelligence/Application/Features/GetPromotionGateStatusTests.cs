using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using GetPromotionGateStatusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPromotionGateStatus.GetPromotionGateStatus;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para GetPromotionGateStatus — retorna estado e avaliações recentes de um gate de promoção.
/// Valida happy path com e sem avaliações, gate não encontrado e validação.
/// </summary>
public sealed class GetPromotionGateStatusTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 15, 0, 0, TimeSpan.Zero);

    private readonly IPromotionGateRepository _gateRepo = Substitute.For<IPromotionGateRepository>();
    private readonly IPromotionGateEvaluationRepository _evalRepo = Substitute.For<IPromotionGateEvaluationRepository>();

    private GetPromotionGateStatusFeature.Handler CreateHandler() =>
        new(_gateRepo, _evalRepo);

    private static PromotionGate MakeGate(string from = "staging", string to = "production") =>
        PromotionGate.Create(
            name: "Staging to Production Gate",
            description: "Validates all criteria before promoting to production.",
            environmentFrom: from,
            environmentTo: to,
            rules: null,
            blockOnFailure: true,
            createdBy: "system",
            createdAt: FixedNow,
            tenantId: "tenant-1");

    private static PromotionGateEvaluation MakeEvaluation(PromotionGateId gateId) =>
        PromotionGateEvaluation.Evaluate(
            gateId: gateId,
            changeId: "change-abc-123",
            result: GateEvaluationResult.Passed,
            ruleResults: null,
            evaluatedAt: FixedNow,
            evaluatedBy: "devops@corp.com",
            tenantId: "tenant-1");

    // ── Happy path — gate found, no evaluations ───────────────────────────────

    [Fact]
    public async Task Handle_GateFound_NoEvaluations_ReturnsEmptyList()
    {
        var gate = MakeGate();
        var gateId = gate.Id;

        _gateRepo.GetByIdAsync(gateId, Arg.Any<CancellationToken>())
            .Returns(gate);
        _evalRepo.ListByGateAsync(gateId, Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGateEvaluation>());

        var result = await CreateHandler().Handle(
            new GetPromotionGateStatusFeature.Query(gateId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GateId.Should().Be(gateId.Value);
        result.Value.Name.Should().Be("Staging to Production Gate");
        result.Value.EnvironmentFrom.Should().Be("staging");
        result.Value.EnvironmentTo.Should().Be("production");
        result.Value.IsActive.Should().BeTrue();
        result.Value.BlockOnFailure.Should().BeTrue();
        result.Value.RecentEvaluations.Should().BeEmpty();
    }

    // ── Happy path — gate with evaluations ───────────────────────────────────

    [Fact]
    public async Task Handle_GateFound_WithEvaluations_ReturnsSummaries()
    {
        var gate = MakeGate();
        var gateId = gate.Id;
        var evaluation = MakeEvaluation(gateId);

        _gateRepo.GetByIdAsync(gateId, Arg.Any<CancellationToken>())
            .Returns(gate);
        _evalRepo.ListByGateAsync(gateId, Arg.Any<CancellationToken>())
            .Returns(new List<PromotionGateEvaluation> { evaluation });

        var result = await CreateHandler().Handle(
            new GetPromotionGateStatusFeature.Query(gateId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RecentEvaluations.Should().HaveCount(1);
        result.Value.RecentEvaluations[0].ChangeId.Should().Be("change-abc-123");
        result.Value.RecentEvaluations[0].Result.Should().Be(GateEvaluationResult.Passed);
        result.Value.RecentEvaluations[0].EvaluatedBy.Should().Be("devops@corp.com");
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_GateNotFound_ReturnsFailure()
    {
        var gateId = PromotionGateId.New();

        _gateRepo.GetByIdAsync(gateId, Arg.Any<CancellationToken>())
            .Returns((PromotionGate?)null);

        var result = await CreateHandler().Handle(
            new GetPromotionGateStatusFeature.Query(gateId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        await _evalRepo.DidNotReceive().ListByGateAsync(Arg.Any<PromotionGateId>(), Arg.Any<CancellationToken>());
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyGateId_ReturnsError()
    {
        var validator = new GetPromotionGateStatusFeature.Validator();
        var result = validator.Validate(new GetPromotionGateStatusFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "GateId");
    }

    [Fact]
    public void Validator_ValidGateId_Passes()
    {
        var validator = new GetPromotionGateStatusFeature.Validator();
        var result = validator.Validate(new GetPromotionGateStatusFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
