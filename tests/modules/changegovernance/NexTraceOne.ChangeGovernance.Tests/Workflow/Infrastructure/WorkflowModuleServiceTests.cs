using NSubstitute;

using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.Workflow.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Enums;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Services;

namespace NexTraceOne.ChangeGovernance.Tests.Workflow.Infrastructure;

public sealed class WorkflowModuleServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IWorkflowInstanceRepository _instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IEvidencePackRepository _evidencePackRepo = Substitute.For<IEvidencePackRepository>();
    private readonly IWorkflowModule _sut;

    public WorkflowModuleServiceTests()
    {
        _sut = new WorkflowModuleService(_instanceRepo, _evidencePackRepo);
    }

    // ── GetWorkflowStatusAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetWorkflowStatusAsync_WhenInstanceExists_ShouldReturnMappedDto()
    {
        var instance = WorkflowInstance.Create(WorkflowTemplateId.New(), Guid.NewGuid(), "deployer@nextraceone.local", FixedNow);

        _instanceRepo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns(instance);

        var result = await _sut.GetWorkflowStatusAsync(instance.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.WorkflowInstanceId.Should().Be(instance.Id.Value);
        result.ReleaseId.Should().Be(instance.ReleaseId);
        result.Status.Should().Be(WorkflowStatus.Draft.ToString());
        result.SubmittedBy.Should().Be("deployer@nextraceone.local");
        result.SubmittedAt.Should().Be(FixedNow);
        result.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WhenInstanceDoesNotExist_ShouldReturnNull()
    {
        var id = WorkflowInstanceId.New();
        _instanceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var result = await _sut.GetWorkflowStatusAsync(id.Value, CancellationToken.None);

        result.Should().BeNull();
    }

    // ── IsReleaseApprovedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task IsReleaseApprovedAsync_WhenApproved_ShouldReturnTrue()
    {
        var releaseId = Guid.NewGuid();
        var instance = WorkflowInstance.Create(WorkflowTemplateId.New(), releaseId, "deployer", FixedNow);
        instance.Advance();
        instance.Complete(WorkflowStatus.Approved, FixedNow);

        _instanceRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(instance);

        var result = await _sut.IsReleaseApprovedAsync(releaseId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsReleaseApprovedAsync_WhenNotApproved_ShouldReturnFalse()
    {
        var releaseId = Guid.NewGuid();
        var instance = WorkflowInstance.Create(WorkflowTemplateId.New(), releaseId, "deployer", FixedNow);

        _instanceRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(instance);

        var result = await _sut.IsReleaseApprovedAsync(releaseId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReleaseApprovedAsync_WhenNoInstance_ShouldReturnFalse()
    {
        _instanceRepo.GetByReleaseIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var result = await _sut.IsReleaseApprovedAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }

    // ── GetEvidencePackAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetEvidencePackAsync_WhenPackExists_ShouldReturnMappedDto()
    {
        var instanceId = WorkflowInstanceId.New();
        var releaseId = Guid.NewGuid();
        var pack = EvidencePack.Create(instanceId, releaseId, FixedNow);
        pack.UpdateScores(0.7m, 0.8m, 0.9m);

        _evidencePackRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(pack);

        var result = await _sut.GetEvidencePackAsync(instanceId.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.EvidencePackId.Should().Be(pack.Id.Value);
        result.WorkflowInstanceId.Should().Be(instanceId.Value);
        result.BlastRadiusScore.Should().Be(0.7m);
        result.SpectralScore.Should().Be(0.8m);
        result.ChangeIntelligenceScore.Should().Be(0.9m);
        result.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetEvidencePackAsync_WhenPackDoesNotExist_ShouldReturnNull()
    {
        var instanceId = WorkflowInstanceId.New();
        _evidencePackRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns((EvidencePack?)null);

        var result = await _sut.GetEvidencePackAsync(instanceId.Value, CancellationToken.None);

        result.Should().BeNull();
    }
}
