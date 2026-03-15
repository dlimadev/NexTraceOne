using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Domain;

/// <summary>
/// Testes unitários para os enums de mitigação do subdomínio Incidents.
/// Verificam definições de valores, contagem e consistência.
/// </summary>
public sealed class MitigationEnumTests
{
    // ── MitigationActionType ─────────────────────────────────────────

    [Fact]
    public void MitigationActionType_ShouldHaveTenValues()
    {
        Enum.GetValues<MitigationActionType>().Should().HaveCount(10);
    }

    [Theory]
    [InlineData(MitigationActionType.Investigate, 0)]
    [InlineData(MitigationActionType.ValidateChange, 1)]
    [InlineData(MitigationActionType.RollbackCandidate, 2)]
    [InlineData(MitigationActionType.RestartControlled, 3)]
    [InlineData(MitigationActionType.Reprocess, 4)]
    [InlineData(MitigationActionType.VerifyDependency, 5)]
    [InlineData(MitigationActionType.Escalate, 6)]
    [InlineData(MitigationActionType.ExecuteRunbook, 7)]
    [InlineData(MitigationActionType.ObserveAndValidate, 8)]
    [InlineData(MitigationActionType.ContractImpactReview, 9)]
    public void MitigationActionType_ShouldHaveExpectedValues(MitigationActionType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    // ── MitigationWorkflowStatus ─────────────────────────────────────

    [Fact]
    public void MitigationWorkflowStatus_ShouldHaveNineValues()
    {
        Enum.GetValues<MitigationWorkflowStatus>().Should().HaveCount(9);
    }

    [Theory]
    [InlineData(MitigationWorkflowStatus.Draft, 0)]
    [InlineData(MitigationWorkflowStatus.Recommended, 1)]
    [InlineData(MitigationWorkflowStatus.AwaitingApproval, 2)]
    [InlineData(MitigationWorkflowStatus.Approved, 3)]
    [InlineData(MitigationWorkflowStatus.InProgress, 4)]
    [InlineData(MitigationWorkflowStatus.AwaitingValidation, 5)]
    [InlineData(MitigationWorkflowStatus.Completed, 6)]
    [InlineData(MitigationWorkflowStatus.Rejected, 7)]
    [InlineData(MitigationWorkflowStatus.Cancelled, 8)]
    public void MitigationWorkflowStatus_ShouldHaveExpectedValues(MitigationWorkflowStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    // ── MitigationDecisionType ───────────────────────────────────────

    [Fact]
    public void MitigationDecisionType_ShouldHaveFourValues()
    {
        Enum.GetValues<MitigationDecisionType>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(MitigationDecisionType.Approved, 0)]
    [InlineData(MitigationDecisionType.Rejected, 1)]
    [InlineData(MitigationDecisionType.Escalated, 2)]
    [InlineData(MitigationDecisionType.Deferred, 3)]
    public void MitigationDecisionType_ShouldHaveExpectedValues(MitigationDecisionType decision, int expected)
    {
        ((int)decision).Should().Be(expected);
    }

    // ── RiskLevel ────────────────────────────────────────────────────

    [Fact]
    public void RiskLevel_ShouldHaveFourValues()
    {
        Enum.GetValues<RiskLevel>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(RiskLevel.Low, 0)]
    [InlineData(RiskLevel.Medium, 1)]
    [InlineData(RiskLevel.High, 2)]
    [InlineData(RiskLevel.Critical, 3)]
    public void RiskLevel_ShouldHaveExpectedValues(RiskLevel level, int expected)
    {
        ((int)level).Should().Be(expected);
    }

    // ── MitigationOutcome ────────────────────────────────────────────

    [Fact]
    public void MitigationOutcome_ShouldHaveFiveValues()
    {
        Enum.GetValues<MitigationOutcome>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(MitigationOutcome.Successful, 0)]
    [InlineData(MitigationOutcome.PartiallySuccessful, 1)]
    [InlineData(MitigationOutcome.Failed, 2)]
    [InlineData(MitigationOutcome.Inconclusive, 3)]
    [InlineData(MitigationOutcome.Cancelled, 4)]
    public void MitigationOutcome_ShouldHaveExpectedValues(MitigationOutcome outcome, int expected)
    {
        ((int)outcome).Should().Be(expected);
    }

    // ── ValidationStatus ─────────────────────────────────────────────

    [Fact]
    public void ValidationStatus_ShouldHaveFiveValues()
    {
        Enum.GetValues<ValidationStatus>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(ValidationStatus.Pending, 0)]
    [InlineData(ValidationStatus.InProgress, 1)]
    [InlineData(ValidationStatus.Passed, 2)]
    [InlineData(ValidationStatus.Failed, 3)]
    [InlineData(ValidationStatus.Skipped, 4)]
    public void ValidationStatus_ShouldHaveExpectedValues(ValidationStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }
}
