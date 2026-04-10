using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade ContractComplianceResult.
/// Valida factory Evaluate, guard clauses, campos opcionais e todos os resultados possíveis.
/// </summary>
public sealed class ContractComplianceResultTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ValidGateId = Guid.NewGuid();

    [Fact]
    public void Evaluate_ValidInputs_ShouldCreateResult()
    {
        var result = CreateValid();

        result.Id.Value.Should().NotBe(Guid.Empty);
        result.GateId.Should().Be(ValidGateId);
        result.ContractVersionId.Should().Be("cv-001");
        result.ChangeId.Should().Be("chg-001");
        result.Result.Should().Be(ComplianceEvaluationResult.Pass);
        result.EvidencePackId.Should().Be("evp-001");
        result.EvaluatedAt.Should().Be(FixedNow);
        result.TenantId.Should().Be("tenant1");
    }

    [Fact]
    public void Evaluate_AllResults_ShouldBeAccepted()
    {
        foreach (var evalResult in Enum.GetValues<ComplianceEvaluationResult>())
        {
            var result = ContractComplianceResult.Evaluate(
                gateId: ValidGateId,
                contractVersionId: "cv-001",
                changeId: null,
                result: evalResult,
                violations: null,
                evidencePackId: null,
                evaluatedAt: FixedNow,
                tenantId: null);

            result.Result.Should().Be(evalResult);
        }
    }

    [Fact]
    public void Evaluate_NullOptionalFields_ShouldBeValid()
    {
        var result = ContractComplianceResult.Evaluate(
            gateId: ValidGateId,
            contractVersionId: "cv-001",
            changeId: null,
            result: ComplianceEvaluationResult.Warn,
            violations: null,
            evidencePackId: null,
            evaluatedAt: FixedNow,
            tenantId: null);

        result.ChangeId.Should().BeNull();
        result.Violations.Should().BeNull();
        result.EvidencePackId.Should().BeNull();
        result.TenantId.Should().BeNull();
    }

    [Fact]
    public void Evaluate_WithViolations_ShouldStoreJsonb()
    {
        var violations = """[{"rule":"HealthScoreMin","expected":50,"actual":30}]""";
        var result = ContractComplianceResult.Evaluate(
            gateId: ValidGateId,
            contractVersionId: "cv-001",
            changeId: null,
            result: ComplianceEvaluationResult.Block,
            violations: violations,
            evidencePackId: null,
            evaluatedAt: FixedNow,
            tenantId: null);

        result.Violations.Should().Be(violations);
        result.Result.Should().Be(ComplianceEvaluationResult.Block);
    }

    // ── Guard clauses ──

    [Fact]
    public void Evaluate_EmptyGateId_ShouldThrow()
    {
        var act = () => ContractComplianceResult.Evaluate(
            gateId: Guid.Empty,
            contractVersionId: "cv-001",
            changeId: null,
            result: ComplianceEvaluationResult.Pass,
            violations: null,
            evidencePackId: null,
            evaluatedAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_NullContractVersionId_ShouldThrow()
    {
        var act = () => ContractComplianceResult.Evaluate(
            gateId: ValidGateId,
            contractVersionId: null!,
            changeId: null,
            result: ComplianceEvaluationResult.Pass,
            violations: null,
            evidencePackId: null,
            evaluatedAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_EmptyContractVersionId_ShouldThrow()
    {
        var act = () => ContractComplianceResult.Evaluate(
            gateId: ValidGateId,
            contractVersionId: "   ",
            changeId: null,
            result: ComplianceEvaluationResult.Pass,
            violations: null,
            evidencePackId: null,
            evaluatedAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_ContractVersionIdTooLong_ShouldThrow()
    {
        var act = () => ContractComplianceResult.Evaluate(
            gateId: ValidGateId,
            contractVersionId: new string('x', 201),
            changeId: null,
            result: ComplianceEvaluationResult.Pass,
            violations: null,
            evidencePackId: null,
            evaluatedAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_ChangeIdTooLong_ShouldThrow()
    {
        var act = () => ContractComplianceResult.Evaluate(
            gateId: ValidGateId,
            contractVersionId: "cv-001",
            changeId: new string('x', 201),
            result: ComplianceEvaluationResult.Pass,
            violations: null,
            evidencePackId: null,
            evaluatedAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_EvidencePackIdTooLong_ShouldThrow()
    {
        var act = () => ContractComplianceResult.Evaluate(
            gateId: ValidGateId,
            contractVersionId: "cv-001",
            changeId: null,
            result: ComplianceEvaluationResult.Pass,
            violations: null,
            evidencePackId: new string('x', 201),
            evaluatedAt: FixedNow,
            tenantId: null);

        act.Should().Throw<ArgumentException>();
    }

    // ── Helper ──

    private ContractComplianceResult CreateValid() => ContractComplianceResult.Evaluate(
        gateId: ValidGateId,
        contractVersionId: "cv-001",
        changeId: "chg-001",
        result: ComplianceEvaluationResult.Pass,
        violations: """[{"rule":"BreakingChangeApproval","status":"passed"}]""",
        evidencePackId: "evp-001",
        evaluatedAt: FixedNow,
        tenantId: "tenant1");
}
