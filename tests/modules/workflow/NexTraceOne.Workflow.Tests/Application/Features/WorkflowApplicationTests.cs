using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;
using CreateWorkflowTemplateFeature = NexTraceOne.Workflow.Application.Features.CreateWorkflowTemplate.CreateWorkflowTemplate;
using InitiateWorkflowFeature = NexTraceOne.Workflow.Application.Features.InitiateWorkflow.InitiateWorkflow;
using ApproveStageFeature = NexTraceOne.Workflow.Application.Features.ApproveStage.ApproveStage;
using RejectWorkflowFeature = NexTraceOne.Workflow.Application.Features.RejectWorkflow.RejectWorkflow;
using GetWorkflowStatusFeature = NexTraceOne.Workflow.Application.Features.GetWorkflowStatus.GetWorkflowStatus;
using GenerateEvidencePackFeature = NexTraceOne.Workflow.Application.Features.GenerateEvidencePack.GenerateEvidencePack;

namespace NexTraceOne.Workflow.Tests.Application.Features;

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
        var unitOfWork = Substitute.For<IUnitOfWork>();
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        templateRepository.GetByIdAsync(Arg.Any<WorkflowTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(template);

        var sut = new InitiateWorkflowFeature.Handler(
            templateRepository, instanceRepository, stageRepository, unitOfWork, dateTimeProvider);

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
        instanceRepository.Received(1).Add(Arg.Any<WorkflowInstance>());
        stageRepository.Received(2).Add(Arg.Any<WorkflowStage>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateWorkflow_Handle_WithInvalidTemplate_ShouldFail()
    {
        var templateRepository = Substitute.For<IWorkflowTemplateRepository>();
        var instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
        var stageRepository = Substitute.For<IWorkflowStageRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        templateRepository.GetByIdAsync(Arg.Any<WorkflowTemplateId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowTemplate?)null);

        var sut = new InitiateWorkflowFeature.Handler(
            templateRepository, instanceRepository, stageRepository, unitOfWork, dateTimeProvider);

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
        var unitOfWork = Substitute.For<IUnitOfWork>();
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
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
}
