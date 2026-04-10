using System.ComponentModel;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Domain.Entities;

/// <summary>Testes unitários da entidade PromotionGateEvaluation.</summary>
public sealed class PromotionGateEvaluationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly PromotionGateId TestGateId = PromotionGateId.New();

    // ── Evaluate com valores válidos ───────────────────────────────────────

    [Fact]
    public void Evaluate_ShouldReturnEvaluation_WithValidValues()
    {
        var evaluation = PromotionGateEvaluation.Evaluate(
            TestGateId,
            "CHG-001",
            GateEvaluationResult.Passed,
            """{"rules":[{"name":"coverage","passed":true}]}""",
            FixedNow,
            "admin@company.com",
            "tenant-1");

        evaluation.Should().NotBeNull();
        evaluation.Id.Value.Should().NotBeEmpty();
        evaluation.GateId.Should().Be(TestGateId);
        evaluation.ChangeId.Should().Be("CHG-001");
        evaluation.Result.Should().Be(GateEvaluationResult.Passed);
        evaluation.RuleResults.Should().Contain("coverage");
        evaluation.EvaluatedAt.Should().Be(FixedNow);
        evaluation.EvaluatedBy.Should().Be("admin@company.com");
        evaluation.TenantId.Should().Be("tenant-1");
    }

    // ── All result types ───────────────────────────────────────────────────

    [Theory]
    [InlineData(GateEvaluationResult.Passed)]
    [InlineData(GateEvaluationResult.Failed)]
    [InlineData(GateEvaluationResult.Warning)]
    public void Evaluate_ShouldAcceptAllValidResults(GateEvaluationResult result)
    {
        var evaluation = PromotionGateEvaluation.Evaluate(
            TestGateId, "CHG-002", result, null, FixedNow, null, null);

        evaluation.Result.Should().Be(result);
    }

    [Fact]
    public void Evaluate_ShouldAllowNullOptionalFields()
    {
        var evaluation = PromotionGateEvaluation.Evaluate(
            TestGateId, "CHG-003", GateEvaluationResult.Warning, null, FixedNow, null, null);

        evaluation.RuleResults.Should().BeNull();
        evaluation.EvaluatedBy.Should().BeNull();
        evaluation.TenantId.Should().BeNull();
    }

    // ── Guard clauses ──────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_ShouldThrow_WhenGateIdIsNull()
    {
        var act = () => PromotionGateEvaluation.Evaluate(
            null!, "CHG-001", GateEvaluationResult.Passed, null, FixedNow, null, null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_ShouldThrow_WhenChangeIdIsNullOrWhitespace(string? changeId)
    {
        var act = () => PromotionGateEvaluation.Evaluate(
            TestGateId, changeId!, GateEvaluationResult.Passed, null, FixedNow, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_ShouldThrow_WhenChangeIdExceedsMaxLength()
    {
        var longChangeId = new string('x', 201);

        var act = () => PromotionGateEvaluation.Evaluate(
            TestGateId, longChangeId, GateEvaluationResult.Passed, null, FixedNow, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_ShouldThrow_WhenResultIsInvalid()
    {
        var act = () => PromotionGateEvaluation.Evaluate(
            TestGateId, "CHG-001", (GateEvaluationResult)99, null, FixedNow, null, null);

        act.Should().Throw<InvalidEnumArgumentException>();
    }

    [Fact]
    public void Evaluate_ShouldThrow_WhenEvaluatedByExceedsMaxLength()
    {
        var longEvaluatedBy = new string('x', 201);

        var act = () => PromotionGateEvaluation.Evaluate(
            TestGateId, "CHG-001", GateEvaluationResult.Passed, null, FixedNow, longEvaluatedBy, null);

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly Typed Id ──────────────────────────────────────────────────

    [Fact]
    public void PromotionGateEvaluationId_New_ShouldGenerateUniqueIds()
    {
        var id1 = PromotionGateEvaluationId.New();
        var id2 = PromotionGateEvaluationId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void PromotionGateEvaluationId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = PromotionGateEvaluationId.From(guid);

        id.Value.Should().Be(guid);
    }
}
