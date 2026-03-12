using NexTraceOne.Promotion.Domain.Entities;
using NexTraceOne.Promotion.Domain.Enums;

namespace NexTraceOne.Promotion.Tests.Domain.Entities;

/// <summary>Testes unitários das entidades de domínio do módulo Promotion.</summary>
public sealed class PromotionDomainTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static DeploymentEnvironment CreateEnvironment(string name = "Production", int order = 3)
        => DeploymentEnvironment.Create(name, "Production environment", order, true, true, FixedNow);

    private static PromotionRequest CreateRequest()
    {
        var srcId = DeploymentEnvironmentId.New();
        var tgtId = DeploymentEnvironmentId.New();
        return PromotionRequest.Create(Guid.NewGuid(), srcId, tgtId, "dev@company.com", FixedNow);
    }

    // ── DeploymentEnvironment ─────────────────────────────────────────────

    [Fact]
    public void DeploymentEnvironment_Create_WithValidArgs_ShouldBeActiveByDefault()
    {
        var env = CreateEnvironment();

        env.IsActive.Should().BeTrue();
        env.Name.Should().Be("Production");
        env.Order.Should().Be(3);
        env.RequiresApproval.Should().BeTrue();
        env.RequiresEvidencePack.Should().BeTrue();
    }

    [Fact]
    public void DeploymentEnvironment_Deactivate_ShouldSetIsActiveFalse()
    {
        var env = CreateEnvironment();
        env.Deactivate();

        env.IsActive.Should().BeFalse();
    }

    [Fact]
    public void DeploymentEnvironment_Activate_AfterDeactivate_ShouldBeActive()
    {
        var env = CreateEnvironment();
        env.Deactivate();
        env.Activate();

        env.IsActive.Should().BeTrue();
    }

    [Fact]
    public void DeploymentEnvironment_SetOrder_WithNegative_ShouldThrow()
    {
        var env = CreateEnvironment();

        var act = () => env.SetOrder(-1);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DeploymentEnvironment_Update_ShouldChangeName()
    {
        var env = CreateEnvironment();
        env.Update("Staging", "Staging description");

        env.Name.Should().Be("Staging");
        env.Description.Should().Be("Staging description");
    }

    // ── PromotionRequest ──────────────────────────────────────────────────

    [Fact]
    public void PromotionRequest_Create_WithValidArgs_ShouldBePending()
    {
        var request = CreateRequest();

        request.Status.Should().Be(PromotionStatus.Pending);
        request.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void PromotionRequest_StartEvaluation_FromPending_ShouldBeInEvaluation()
    {
        var request = CreateRequest();

        var result = request.StartEvaluation();

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(PromotionStatus.InEvaluation);
    }

    [Fact]
    public void PromotionRequest_Approve_FromInEvaluation_ShouldBeApproved()
    {
        var request = CreateRequest();
        request.StartEvaluation();

        var result = request.Approve(FixedNow);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(PromotionStatus.Approved);
        request.CompletedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void PromotionRequest_Approve_FromPending_ShouldFail()
    {
        var request = CreateRequest();

        var result = request.Approve(FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public void PromotionRequest_Reject_FromInEvaluation_ShouldBeRejected()
    {
        var request = CreateRequest();
        request.StartEvaluation();

        var result = request.Reject(FixedNow);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(PromotionStatus.Rejected);
    }

    [Fact]
    public void PromotionRequest_Block_FromInEvaluation_ShouldBeBlocked()
    {
        var request = CreateRequest();
        request.StartEvaluation();

        var result = request.Block(FixedNow);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(PromotionStatus.Blocked);
    }

    [Fact]
    public void PromotionRequest_Cancel_FromPending_ShouldBeCancelled()
    {
        var request = CreateRequest();

        var result = request.Cancel(FixedNow);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(PromotionStatus.Cancelled);
    }

    [Fact]
    public void PromotionRequest_Approve_WhenAlreadyCompleted_ShouldFail()
    {
        var request = CreateRequest();
        request.StartEvaluation();
        request.Approve(FixedNow);

        var result = request.Approve(FixedNow.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyCompleted");
    }

    [Fact]
    public void PromotionRequest_SetJustification_ShouldPersist()
    {
        var request = CreateRequest();
        request.SetJustification("Critical hotfix deployment");

        request.Justification.Should().Be("Critical hotfix deployment");
    }

    // ── PromotionGate ──────────────────────────────────────────────────────

    [Fact]
    public void PromotionGate_Create_WithValidArgs_ShouldBeActiveByDefault()
    {
        var envId = DeploymentEnvironmentId.New();
        var gate = PromotionGate.Create(envId, "AllTestsPassed", "Quality", true);

        gate.IsActive.Should().BeTrue();
        gate.IsRequired.Should().BeTrue();
        gate.GateName.Should().Be("AllTestsPassed");
        gate.GateType.Should().Be("Quality");
    }

    [Fact]
    public void PromotionGate_Deactivate_ShouldSetIsActiveFalse()
    {
        var envId = DeploymentEnvironmentId.New();
        var gate = PromotionGate.Create(envId, "ScanPassed", "Security", false);
        gate.Deactivate();

        gate.IsActive.Should().BeFalse();
    }

    // ── GateEvaluation ────────────────────────────────────────────────────

    [Fact]
    public void GateEvaluation_Create_WithValidArgs_ShouldStoreValues()
    {
        var requestId = PromotionRequestId.New();
        var gateId = PromotionGateId.New();

        var evaluation = GateEvaluation.Create(requestId, gateId, true, "sys@company.com", "All OK", FixedNow);

        evaluation.Passed.Should().BeTrue();
        evaluation.EvaluatedBy.Should().Be("sys@company.com");
        evaluation.EvaluationDetails.Should().Be("All OK");
        evaluation.OverrideJustification.Should().BeNull();
    }

    [Fact]
    public void GateEvaluation_Override_ShouldMarkAsPassedWithJustification()
    {
        var requestId = PromotionRequestId.New();
        var gateId = PromotionGateId.New();
        var evaluation = GateEvaluation.Create(requestId, gateId, false, "sys@company.com", null, FixedNow);

        evaluation.Override("Emergency override — VP approved", "vp@company.com", FixedNow.AddMinutes(5));

        evaluation.Passed.Should().BeTrue();
        evaluation.OverrideJustification.Should().Be("Emergency override — VP approved");
        evaluation.EvaluatedBy.Should().Be("vp@company.com");
    }
}
