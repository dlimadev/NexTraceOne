using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;

namespace NexTraceOne.Workflow.Tests.Domain.Entities;

/// <summary>Testes unitários das entidades de domínio do módulo Workflow.</summary>
public sealed class WorkflowDomainTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static WorkflowTemplate CreateTemplate() =>
        WorkflowTemplate.Create("Release Approval", "Template para aprovação de releases", "Breaking", "High", "Production", 2, FixedNow);

    private static WorkflowInstance CreateInstance() =>
        WorkflowInstance.Create(WorkflowTemplateId.New(), Guid.NewGuid(), "user@company.com", FixedNow);

    private static WorkflowStage CreateStage(WorkflowInstanceId? instanceId = null) =>
        WorkflowStage.Create(instanceId ?? WorkflowInstanceId.New(), "Code Review", 0, 2, true, 24);

    // ── WorkflowTemplate ──────────────────────────────────────────────────

    [Fact]
    public void Template_Create_WithValidData_ShouldSucceed()
    {
        var template = CreateTemplate();

        template.Should().NotBeNull();
        template.Name.Should().Be("Release Approval");
        template.Description.Should().Be("Template para aprovação de releases");
        template.ChangeType.Should().Be("Breaking");
        template.ApiCriticality.Should().Be("High");
        template.TargetEnvironment.Should().Be("Production");
        template.MinimumApprovers.Should().Be(2);
        template.IsActive.Should().BeTrue();
        template.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Template_Create_WithEmptyName_ShouldFail()
    {
        var act = () => WorkflowTemplate.Create("", "desc", "Breaking", "High", "Production", 1, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Template_Update_ShouldModifyProperties()
    {
        var template = CreateTemplate();

        template.Update("New Name", "New Description");

        template.Name.Should().Be("New Name");
        template.Description.Should().Be("New Description");
    }

    [Fact]
    public void Template_Activate_Deactivate_ShouldToggle()
    {
        var template = CreateTemplate();
        template.IsActive.Should().BeTrue();

        template.Deactivate();
        template.IsActive.Should().BeFalse();

        template.Activate();
        template.IsActive.Should().BeTrue();
    }

    // ── WorkflowInstance ──────────────────────────────────────────────────

    [Fact]
    public void Instance_Create_WithValidData_ShouldSucceed()
    {
        var templateId = WorkflowTemplateId.New();
        var releaseId = Guid.NewGuid();

        var instance = WorkflowInstance.Create(templateId, releaseId, "user@company.com", FixedNow);

        instance.Should().NotBeNull();
        instance.WorkflowTemplateId.Should().Be(templateId);
        instance.ReleaseId.Should().Be(releaseId);
        instance.SubmittedBy.Should().Be("user@company.com");
        instance.Status.Should().Be(WorkflowStatus.Draft);
        instance.CurrentStageIndex.Should().Be(0);
        instance.SubmittedAt.Should().Be(FixedNow);
        instance.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Instance_Advance_ShouldIncrementStageIndex()
    {
        var instance = CreateInstance();

        var result = instance.Advance();

        result.IsSuccess.Should().BeTrue();
        instance.CurrentStageIndex.Should().Be(1);
        instance.Status.Should().Be(WorkflowStatus.InReview);
    }

    [Fact]
    public void Instance_Complete_ShouldSetStatus()
    {
        var instance = CreateInstance();
        instance.Advance();

        var result = instance.Complete(WorkflowStatus.Approved, FixedNow);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowStatus.Approved);
        instance.CompletedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Instance_Cancel_ShouldSetStatus()
    {
        var instance = CreateInstance();

        var result = instance.Cancel(FixedNow);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
        instance.CompletedAt.Should().Be(FixedNow);
    }

    // ── WorkflowStage ─────────────────────────────────────────────────────

    [Fact]
    public void Stage_Create_WithValidData_ShouldSucceed()
    {
        var instanceId = WorkflowInstanceId.New();

        var stage = WorkflowStage.Create(instanceId, "Security Review", 1, 3, false, 48);

        stage.Should().NotBeNull();
        stage.WorkflowInstanceId.Should().Be(instanceId);
        stage.Name.Should().Be("Security Review");
        stage.StageOrder.Should().Be(1);
        stage.Status.Should().Be(StageStatus.Pending);
        stage.RequiredApprovers.Should().Be(3);
        stage.CurrentApprovals.Should().Be(0);
        stage.CommentRequired.Should().BeFalse();
        stage.SlaDurationHours.Should().Be(48);
    }

    [Fact]
    public void Stage_Start_ShouldSetStatus()
    {
        var stage = CreateStage();

        stage.Start(FixedNow);

        stage.Status.Should().Be(StageStatus.InReview);
        stage.StartedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Stage_RecordApproval_ShouldIncrementApprovals()
    {
        var stage = CreateStage();
        stage.Start(FixedNow);

        var result = stage.RecordApproval(FixedNow);

        result.IsSuccess.Should().BeTrue();
        stage.CurrentApprovals.Should().Be(1);
        stage.Status.Should().Be(StageStatus.InReview);
    }

    [Fact]
    public void Stage_RecordApproval_WhenReachingRequired_ShouldApprove()
    {
        var stage = WorkflowStage.Create(WorkflowInstanceId.New(), "Review", 0, 1, false, null);
        stage.Start(FixedNow);

        var result = stage.RecordApproval(FixedNow);

        result.IsSuccess.Should().BeTrue();
        stage.Status.Should().Be(StageStatus.Approved);
        stage.CompletedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Stage_RecordRejection_ShouldSetRejected()
    {
        var stage = CreateStage();
        stage.Start(FixedNow);

        var result = stage.RecordRejection(FixedNow);

        result.IsSuccess.Should().BeTrue();
        stage.Status.Should().Be(StageStatus.Rejected);
        stage.CompletedAt.Should().Be(FixedNow);
    }

    // ── ApprovalDecision ──────────────────────────────────────────────────

    [Fact]
    public void ApprovalDecision_Create_WithApproval_ShouldSucceed()
    {
        var result = ApprovalDecision.Create(
            WorkflowStageId.New(),
            WorkflowInstanceId.New(),
            "approver@company.com",
            ApprovalAction.Approved,
            "Looks good",
            FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(ApprovalAction.Approved);
        result.Value.DecidedBy.Should().Be("approver@company.com");
        result.Value.Comment.Should().Be("Looks good");
    }

    [Fact]
    public void ApprovalDecision_Create_Rejection_WithoutComment_ShouldFail()
    {
        var result = ApprovalDecision.Create(
            WorkflowStageId.New(),
            WorkflowInstanceId.New(),
            "approver@company.com",
            ApprovalAction.Rejected,
            null,
            FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CommentRequiredForRejection");
    }

    // ── EvidencePack ──────────────────────────────────────────────────────

    [Fact]
    public void EvidencePack_Create_ShouldSucceed()
    {
        var instanceId = WorkflowInstanceId.New();
        var releaseId = Guid.NewGuid();

        var pack = EvidencePack.Create(instanceId, releaseId, FixedNow);

        pack.Should().NotBeNull();
        pack.WorkflowInstanceId.Should().Be(instanceId);
        pack.ReleaseId.Should().Be(releaseId);
        pack.CompletenessPercentage.Should().Be(0m);
        pack.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void EvidencePack_UpdateScores_ShouldRecalculateCompleteness()
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), Guid.NewGuid(), FixedNow);

        pack.UpdateScores(0.5m, 0.8m, 0.3m);

        pack.BlastRadiusScore.Should().Be(0.5m);
        pack.SpectralScore.Should().Be(0.8m);
        pack.ChangeIntelligenceScore.Should().Be(0.3m);
        pack.CompletenessPercentage.Should().BeGreaterThan(0m);
    }
}
