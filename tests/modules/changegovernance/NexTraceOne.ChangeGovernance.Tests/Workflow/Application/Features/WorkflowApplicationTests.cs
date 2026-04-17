using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Enums;

using AttachCiCdEvidenceFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.AttachCiCdEvidence.AttachCiCdEvidence;
using CreateWorkflowTemplateFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.CreateWorkflowTemplate.CreateWorkflowTemplate;
using InitiateWorkflowFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.InitiateWorkflow.InitiateWorkflow;
using ApproveStageFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.ApproveStage.ApproveStage;
using RejectWorkflowFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.RejectWorkflow.RejectWorkflow;
using RequestChangesFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.RequestChanges.RequestChanges;
using AddObservationFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.AddObservation.AddObservation;
using GetWorkflowStatusFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetWorkflowStatus.GetWorkflowStatus;
using ListPendingApprovalsFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.ListPendingApprovals.ListPendingApprovals;
using GenerateEvidencePackFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.GenerateEvidencePack.GenerateEvidencePack;
using GetEvidencePackFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetEvidencePack.GetEvidencePack;
using ExportEvidencePackPdfFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.ExportEvidencePackPdf.ExportEvidencePackPdf;
using EscalateSlaViolationFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.EscalateSlaViolation.EscalateSlaViolation;

namespace NexTraceOne.ChangeGovernance.Tests.Workflow.Application.Features;

/// <summary>Testes de handlers da camada Application do módulo Workflow.</summary>
public sealed class WorkflowApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static WorkflowTemplate CreateTemplate() =>
        WorkflowTemplate.Create("Release Approval", "desc", "Breaking", "High", "Production", 2, FixedNow);

    private static WorkflowInstance CreateInstance(WorkflowTemplateId? templateId = null) =>
        WorkflowInstance.Create(templateId ?? WorkflowTemplateId.New(), Guid.NewGuid(), "submitter@company.com", FixedNow);

    private static WorkflowStage CreateStage(WorkflowInstanceId instanceId) =>
        WorkflowStage.Create(instanceId, "Code Review", 0, 1, false, null);

    // ── CreateWorkflowTemplate ────────────────────────────────────────────

    [Fact]
    public async Task CreateWorkflowTemplate_Handle_WithValidCommand_ShouldCreateTemplate()
    {
        var repository = Substitute.For<IWorkflowTemplateRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);
        var sut = new CreateWorkflowTemplateFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var command = new CreateWorkflowTemplateFeature.Command(
            "API Approval", "Template for API changes", "Breaking", "Critical", "Production", 3);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("API Approval");
        result.Value.IsActive.Should().BeTrue();
        repository.Received(1).Add(Arg.Any<WorkflowTemplate>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateWorkflowTemplate_Handle_WithInvalidName_ShouldFail()
    {
        var validator = new CreateWorkflowTemplateFeature.Validator();
        var command = new CreateWorkflowTemplateFeature.Command(
            "", "desc", "Breaking", "High", "Production", 1);

        var validationResult = validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── InitiateWorkflow ──────────────────────────────────────────────────

    [Fact]
    public async Task InitiateWorkflow_Handle_WithValidCommand_ShouldCreateInstance()
    {
        var template = CreateTemplate();
        var templateRepository = Substitute.For<IWorkflowTemplateRepository>();
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        templateRepository.GetByIdAsync(Arg.Any<WorkflowTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(template);

        var sut = new InitiateWorkflowFeature.Handler(
            templateRepository, instanceRepository, stageRepository, evidencePackRepository, unitOfWork, dateTimeProvider);

        var stages = new List<InitiateWorkflowFeature.StageInput>
        {
            new("Code Review", 2, true),
            new("Security Review", 1, false)
        };

        var command = new InitiateWorkflowFeature.Command(
            template.Id.Value, Guid.NewGuid(), "user@company.com", stages);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WorkflowStatus.Draft.ToString());
        result.Value.StagesCreated.Should().Be(2);
        result.Value.EvidencePackId.Should().NotBe(Guid.Empty);
        instanceRepository.Received(1).Add(Arg.Any<WorkflowInstance>());
        stageRepository.Received(2).Add(Arg.Any<WorkflowStage>());
        evidencePackRepository.Received(1).Add(Arg.Any<EvidencePack>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateWorkflow_Handle_WithInvalidTemplate_ShouldFail()
    {
        var templateRepository = Substitute.For<IWorkflowTemplateRepository>();
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        templateRepository.GetByIdAsync(Arg.Any<WorkflowTemplateId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowTemplate?)null);

        var sut = new InitiateWorkflowFeature.Handler(
            templateRepository, instanceRepository, stageRepository, evidencePackRepository, unitOfWork, dateTimeProvider);

        var command = new InitiateWorkflowFeature.Command(
            Guid.NewGuid(), Guid.NewGuid(), "user@company.com",
            new List<InitiateWorkflowFeature.StageInput> { new("Review", 1, false) });

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Template.NotFound");
    }

    // ── ApproveStage ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveStage_Handle_WithValidData_ShouldApproveStage()
    {
        var instance = CreateInstance();
        instance.Advance();
        var stage = CreateStage(instance.Id);
        stage.Start(FixedNow);

        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns(stage);
        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.ListByInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowStage> { stage });

        var sut = new ApproveStageFeature.Handler(
            stageRepository, instanceRepository, decisionRepository, unitOfWork, dateTimeProvider);

        var command = new ApproveStageFeature.Command(stage.Id.Value, "approver@company.com", "LGTM");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StageCompleted.Should().BeTrue();
        decisionRepository.Received(1).Add(Arg.Any<ApprovalDecision>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveStage_Handle_StageNotFound_ShouldFail()
    {
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowStage?)null);

        var sut = new ApproveStageFeature.Handler(
            stageRepository, instanceRepository, decisionRepository, unitOfWork, dateTimeProvider);

        var command = new ApproveStageFeature.Command(Guid.NewGuid(), "approver@company.com", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Stage.NotFound");
    }

    // ── RejectWorkflow ────────────────────────────────────────────────────

    [Fact]
    public async Task RejectWorkflow_Handle_WithComment_ShouldRejectWorkflow()
    {
        var instance = CreateInstance();
        instance.Advance();
        var stage = CreateStage(instance.Id);
        stage.Start(FixedNow);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns(stage);

        var sut = new RejectWorkflowFeature.Handler(
            instanceRepository, stageRepository, decisionRepository, unitOfWork, dateTimeProvider);

        var command = new RejectWorkflowFeature.Command(
            instance.Id.Value, stage.Id.Value, "reviewer@company.com", "Security concerns found");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkflowStatus.Should().Be(WorkflowStatus.Rejected.ToString());
        decisionRepository.Received(1).Add(Arg.Any<ApprovalDecision>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RejectWorkflow_Handle_WithoutComment_ShouldFail()
    {
        var validator = new RejectWorkflowFeature.Validator();
        var command = new RejectWorkflowFeature.Command(
            Guid.NewGuid(), Guid.NewGuid(), "reviewer@company.com", "");

        var validationResult = validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Comment");
    }

    // ── GetWorkflowStatus ─────────────────────────────────────────────────

    [Fact]
    public async Task GetWorkflowStatus_Handle_ShouldReturnStatus()
    {
        var instance = CreateInstance();
        var stage = CreateStage(instance.Id);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.ListByInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowStage> { stage });

        var sut = new GetWorkflowStatusFeature.Handler(instanceRepository, stageRepository);

        var result = await sut.Handle(
            new GetWorkflowStatusFeature.Query(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InstanceId.Should().Be(instance.Id.Value);
        result.Value.Status.Should().Be(WorkflowStatus.Draft.ToString());
        result.Value.SubmittedBy.Should().Be("submitter@company.com");
        result.Value.Stages.Should().HaveCount(1);
    }

    // ── GenerateEvidencePack ──────────────────────────────────────────────

    [Fact]
    public async Task GenerateEvidencePack_Handle_ShouldCreateEvidencePack()
    {
        var instance = CreateInstance();

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        evidencePackRepository.GetByWorkflowInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns((EvidencePack?)null);

        var sut = new GenerateEvidencePackFeature.Handler(
            instanceRepository, evidencePackRepository, unitOfWork, dateTimeProvider);

        var command = new GenerateEvidencePackFeature.Command(
            instance.Id.Value, "Breaking change in endpoint /api/users", 0.7m, 0.9m, 0.5m, "sha256hash");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeTrue();
        result.Value.CompletenessPercentage.Should().BeGreaterThan(0m);
        evidencePackRepository.Received(1).Add(Arg.Any<EvidencePack>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── RequestChanges ────────────────────────────────────────────────────

    [Fact]
    public async Task RequestChanges_Handle_WithComment_ShouldRecordDecision()
    {
        var instance = CreateInstance();
        instance.Advance();
        var stage = CreateStage(instance.Id);
        stage.Start(FixedNow);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns(stage);

        var sut = new RequestChangesFeature.Handler(
            instanceRepository, stageRepository, decisionRepository, unitOfWork, dateTimeProvider);

        var command = new RequestChangesFeature.Command(
            instance.Id.Value,
            stage.Id.Value,
            "reviewer@company.com",
            "Please fix these issues",
            new List<string> { "Missing tests", "Remove debug logs" });

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedItemsCount.Should().Be(2);
        decisionRepository.Received(1).Add(Arg.Any<ApprovalDecision>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RequestChanges_Handle_WithoutComment_ShouldFail()
    {
        var validator = new RequestChangesFeature.Validator();
        var command = new RequestChangesFeature.Command(
            Guid.NewGuid(), Guid.NewGuid(), "reviewer@company.com", "",
            new List<string> { "Item 1" });

        var validationResult = validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Comment");
    }

    [Fact]
    public async Task RequestChanges_Handle_InstanceNotFound_ShouldFail()
    {
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var sut = new RequestChangesFeature.Handler(
            instanceRepository, stageRepository, decisionRepository, unitOfWork, dateTimeProvider);

        var command = new RequestChangesFeature.Command(
            Guid.NewGuid(), Guid.NewGuid(), "reviewer@company.com",
            "Some comment", new List<string> { "Item 1" });

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Instance.NotFound");
    }

    // ── AddObservation ────────────────────────────────────────────────────

    [Fact]
    public async Task AddObservation_Handle_ShouldRecordObservation()
    {
        var instance = CreateInstance();
        var stage = CreateStage(instance.Id);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns(stage);

        var sut = new AddObservationFeature.Handler(
            instanceRepository, stageRepository, decisionRepository, unitOfWork, dateTimeProvider);

        var command = new AddObservationFeature.Command(
            instance.Id.Value,
            stage.Id.Value,
            "observer@company.com",
            "This change looks interesting from a performance perspective.");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StageId.Should().Be(stage.Id.Value);
        decisionRepository.Received(1).Add(Arg.Any<ApprovalDecision>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddObservation_Handle_StageNotFound_ShouldFail()
    {
        var instance = CreateInstance();

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowStage?)null);

        var sut = new AddObservationFeature.Handler(
            instanceRepository, stageRepository, decisionRepository, unitOfWork, dateTimeProvider);

        var command = new AddObservationFeature.Command(
            instance.Id.Value, Guid.NewGuid(), "observer@company.com", "Some observation");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Stage.NotFound");
    }

    // ── ListPendingApprovals ──────────────────────────────────────────────

    [Fact]
    public async Task ListPendingApprovals_Handle_ShouldReturnPaginatedInstances()
    {
        var instance1 = CreateInstance();
        instance1.Advance();
        var instance2 = CreateInstance();
        instance2.Advance();

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepository.ListByStatusAsync(WorkflowStatus.InReview, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowInstance> { instance1, instance2 });
        instanceRepository.CountByStatusAsync(WorkflowStatus.InReview, Arg.Any<CancellationToken>())
            .Returns(2);

        var sut = new ListPendingApprovalsFeature.Handler(instanceRepository);

        var result = await sut.Handle(
            new ListPendingApprovalsFeature.Query("approver@company.com", 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task ListPendingApprovals_Handle_NoPending_ShouldReturnEmptyList()
    {
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepository.ListByStatusAsync(Arg.Any<WorkflowStatus>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowInstance>());
        instanceRepository.CountByStatusAsync(Arg.Any<WorkflowStatus>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = new ListPendingApprovalsFeature.Handler(instanceRepository);

        var result = await sut.Handle(
            new ListPendingApprovalsFeature.Query("approver@company.com", 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetEvidencePack ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEvidencePack_Handle_ShouldReturnPack()
    {
        var instance = CreateInstance();
        var pack = EvidencePack.Create(instance.Id, instance.ReleaseId, FixedNow);
        pack.UpdateScores(0.4m, 0.7m, 0.6m);
        pack.SetContractDiff("Breaking change: removed field");

        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        evidencePackRepository.GetByWorkflowInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(pack);

        var sut = new GetEvidencePackFeature.Handler(evidencePackRepository);

        var result = await sut.Handle(
            new GetEvidencePackFeature.Query(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkflowInstanceId.Should().Be(instance.Id.Value);
        result.Value.BlastRadiusScore.Should().Be(0.4m);
        result.Value.SpectralScore.Should().Be(0.7m);
        result.Value.ChangeIntelligenceScore.Should().Be(0.6m);
        result.Value.ContractDiffSummary.Should().Be("Breaking change: removed field");
        result.Value.CompletenessPercentage.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task GetEvidencePack_Handle_NotFound_ShouldFail()
    {
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        evidencePackRepository.GetByWorkflowInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns((EvidencePack?)null);

        var sut = new GetEvidencePackFeature.Handler(evidencePackRepository);

        var result = await sut.Handle(
            new GetEvidencePackFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EvidencePack.NotFound");
    }

    // ── ExportEvidencePackPdf ─────────────────────────────────────────────

    [Fact]
    public async Task ExportEvidencePackPdf_Handle_ShouldReturnStructuredData()
    {
        var instance = CreateInstance();
        var stage = CreateStage(instance.Id);
        var pack = EvidencePack.Create(instance.Id, instance.ReleaseId, FixedNow);
        pack.UpdateScores(0.5m, 0.8m, 0.6m);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        evidencePackRepository.GetByWorkflowInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        stageRepository.ListByInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowStage> { stage });
        decisionRepository.ListByInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApprovalDecision>());

        var sut = new ExportEvidencePackPdfFeature.Handler(
            instanceRepository, evidencePackRepository, stageRepository, decisionRepository);

        var result = await sut.Handle(
            new ExportEvidencePackPdfFeature.Query(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkflowInstanceId.Should().Be(instance.Id.Value);
        result.Value.ReleaseId.Should().Be(instance.ReleaseId);
        result.Value.Stages.Should().HaveCount(1);
        result.Value.Decisions.Should().BeEmpty();
        result.Value.BlastRadiusScore.Should().Be(0.5m);
    }

    [Fact]
    public async Task ExportEvidencePackPdf_Handle_EvidencePackNotFound_ShouldFail()
    {
        var instance = CreateInstance();

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var decisionRepository = Substitute.For<IApprovalDecisionRepository>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        evidencePackRepository.GetByWorkflowInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns((EvidencePack?)null);

        var sut = new ExportEvidencePackPdfFeature.Handler(
            instanceRepository, evidencePackRepository, stageRepository, decisionRepository);

        var result = await sut.Handle(
            new ExportEvidencePackPdfFeature.Query(instance.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EvidencePack.NotFound");
    }

    // ── AttachCiCdEvidence ────────────────────────────────────────────────

    [Fact]
    public async Task AttachCiCdEvidence_Handle_ShouldUpdatePack_WithPipelineData()
    {
        var instance = CreateInstance();
        var pack = EvidencePack.Create(instance.Id, instance.ReleaseId, FixedNow);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        evidencePackRepository.GetByWorkflowInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(pack);

        var sut = new AttachCiCdEvidenceFeature.Handler(instanceRepository, evidencePackRepository, unitOfWork);

        var command = new AttachCiCdEvidenceFeature.Command(
            instance.Id.Value,
            PipelineSource: "github-actions",
            BuildId: "run-12345",
            CommitSha: "cafebabe1234",
            CiChecksResult: "passed");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PipelineSource.Should().Be("github-actions");
        result.Value.BuildId.Should().Be("run-12345");
        result.Value.CommitSha.Should().Be("cafebabe1234");
        result.Value.CiChecksResult.Should().Be("passed");
        result.Value.CompletenessPercentage.Should().BeGreaterThan(0m);
        evidencePackRepository.Received(1).Update(Arg.Any<EvidencePack>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AttachCiCdEvidence_Handle_WhenInstanceNotFound_ShouldFail()
    {
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var sut = new AttachCiCdEvidenceFeature.Handler(instanceRepository, evidencePackRepository, unitOfWork);

        var result = await sut.Handle(
            new AttachCiCdEvidenceFeature.Command(Guid.NewGuid(), "github-actions", null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Instance.NotFound");
    }

    [Fact]
    public async Task AttachCiCdEvidence_Handle_WhenPackNotFound_ShouldFail()
    {
        var instance = CreateInstance();
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var evidencePackRepository = Substitute.For<IEvidencePackRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        evidencePackRepository.GetByWorkflowInstanceIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns((EvidencePack?)null);

        var sut = new AttachCiCdEvidenceFeature.Handler(instanceRepository, evidencePackRepository, unitOfWork);

        var result = await sut.Handle(
            new AttachCiCdEvidenceFeature.Command(instance.Id.Value, "jenkins", "build-99", "abc123", "failed"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EvidencePack.NotFound");
    }

    // ── EscalateSlaViolation ──────────────────────────────────────────────

    [Fact]
    public async Task EscalateSlaViolation_Handle_WithMatchingPolicy_ShouldReturnEscalation()
    {
        var template = CreateTemplate();
        var instance = CreateInstance(template.Id);
        var stage = CreateStage(instance.Id);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var slaPolicyRepository = Substitute.For<ISlaPolicyRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns(stage);

        var slaPolicy = SlaPolicy.Create(template.Id, "Code Review", 24, true, "TechLead");
        slaPolicyRepository.GetByTemplateIdAsync(Arg.Any<WorkflowTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(new List<SlaPolicy> { slaPolicy });

        var sut = new EscalateSlaViolationFeature.Handler(
            instanceRepository, stageRepository, slaPolicyRepository, unitOfWork, dateTimeProvider);

        var command = new EscalateSlaViolationFeature.Command(
            instance.Id.Value, stage.Id.Value, "SLA expired after 24 hours");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkflowInstanceId.Should().Be(instance.Id.Value);
        result.Value.EscalationTargetRole.Should().Be("TechLead");
        result.Value.EscalatedPolicies.Should().HaveCount(1);
    }

    [Fact]
    public async Task EscalateSlaViolation_Handle_NoMatchingPolicy_ShouldReturnNoEscalationTarget()
    {
        var instance = CreateInstance();
        var stage = CreateStage(instance.Id);

        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var slaPolicyRepository = Substitute.For<ISlaPolicyRepository>();
        var unitOfWork = Substitute.For<IWorkflowUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        instanceRepository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns(instance);
        stageRepository.GetByIdAsync(Arg.Any<WorkflowStageId>(), Arg.Any<CancellationToken>())
            .Returns(stage);
        slaPolicyRepository.GetByTemplateIdAsync(Arg.Any<WorkflowTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(new List<SlaPolicy>());

        var sut = new EscalateSlaViolationFeature.Handler(
            instanceRepository, stageRepository, slaPolicyRepository, unitOfWork, dateTimeProvider);

        var command = new EscalateSlaViolationFeature.Command(
            instance.Id.Value, stage.Id.Value, "Stage overdue");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EscalationTargetRole.Should().BeNull();
        result.Value.EscalatedPolicies.Should().BeEmpty();
    }
}
