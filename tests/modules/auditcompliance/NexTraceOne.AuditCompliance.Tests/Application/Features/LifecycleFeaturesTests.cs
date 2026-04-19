using Microsoft.Extensions.Logging;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.ActivateCompliancePolicy;
using NexTraceOne.AuditCompliance.Application.Features.DeactivateCompliancePolicy;
using NexTraceOne.AuditCompliance.Application.Features.TransitionAuditCampaign;
using NexTraceOne.AuditCompliance.Application.Features.UpdateCompliancePolicy;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de lifecycle de CompliancePolicy e AuditCampaign.
/// Cobre UpdateCompliancePolicy, ActivateCompliancePolicy, DeactivateCompliancePolicy
/// e TransitionAuditCampaign (Start/Complete/Cancel).
/// </summary>
public sealed class LifecycleFeaturesTests
{
    private readonly ICompliancePolicyRepository _policyRepository = Substitute.For<ICompliancePolicyRepository>();
    private readonly IAuditCampaignRepository _campaignRepository = Substitute.For<IAuditCampaignRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IAuditComplianceUnitOfWork _unitOfWork = Substitute.For<IAuditComplianceUnitOfWork>();

    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    public LifecycleFeaturesTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── Factory helpers ──

    private static CompliancePolicy MakePolicy(bool isActive = true)
    {
        var p = CompliancePolicy.Create(
            "test-policy", "Test Policy", null,
            "Security", ComplianceSeverity.High, null,
            TenantId, FixedNow);
        if (!isActive)
            p.Deactivate(FixedNow);
        return p;
    }

    private static AuditCampaign MakeCampaign(CampaignStatus status = CampaignStatus.Planned)
    {
        var c = AuditCampaign.Create("Campaign-1", null, "Periodic", null, TenantId, "admin@org.com", FixedNow);
        if (status == CampaignStatus.InProgress)
            c.Start(FixedNow);
        if (status == CampaignStatus.Completed)
        {
            c.Start(FixedNow);
            c.Complete(FixedNow);
        }
        if (status == CampaignStatus.Cancelled)
            c.Cancel(FixedNow);
        return c;
    }

    // ══════════════════════════════════════════════════════════════
    // UpdateCompliancePolicy
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateCompliancePolicy_PolicyExists_ShouldUpdateAndPersist()
    {
        var policy = MakePolicy();
        var policyId = policy.Id;

        _policyRepository.GetByIdAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new UpdateCompliancePolicy.Handler(_policyRepository, _clock, _unitOfWork);
        var command = new UpdateCompliancePolicy.Command(policyId.Value, "New Name", "Updated desc", "Privacy", "Critical", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("New Name");
        result.Value.Category.Should().Be("Privacy");
        result.Value.Severity.Should().Be("Critical");

        _policyRepository.Received(1).Update(policy);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCompliancePolicy_PolicyNotFound_ShouldReturnError()
    {
        _policyRepository.GetByIdAsync(Arg.Any<CompliancePolicyId>(), Arg.Any<CancellationToken>())
            .Returns((CompliancePolicy?)null);

        var handler = new UpdateCompliancePolicy.Handler(_policyRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new UpdateCompliancePolicy.Command(Guid.NewGuid(), "Name", null, "Security", "High", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _policyRepository.DidNotReceive().Update(Arg.Any<CompliancePolicy>());
    }

    [Fact]
    public void UpdateCompliancePolicyValidator_EmptyDisplayName_ShouldFail()
    {
        var v = new UpdateCompliancePolicy.Validator();
        v.Validate(new UpdateCompliancePolicy.Command(Guid.NewGuid(), "", null, "Security", "High", null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateCompliancePolicyValidator_InvalidSeverity_ShouldFail()
    {
        var v = new UpdateCompliancePolicy.Validator();
        v.Validate(new UpdateCompliancePolicy.Command(Guid.NewGuid(), "Name", null, "Security", "INVALID", null))
            .IsValid.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // ActivateCompliancePolicy
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ActivateCompliancePolicy_WhenDeactivated_ShouldActivate()
    {
        var policy = MakePolicy(isActive: false);
        policy.IsActive.Should().BeFalse();

        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new ActivateCompliancePolicy.Handler(_policyRepository, _clock, _unitOfWork);
        var result = await handler.Handle(new ActivateCompliancePolicy.Command(policy.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        _policyRepository.Received(1).Update(policy);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateCompliancePolicy_WhenAlreadyActive_ShouldBeIdempotent()
    {
        var policy = MakePolicy(isActive: true);

        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new ActivateCompliancePolicy.Handler(_policyRepository, _clock, _unitOfWork);
        var result = await handler.Handle(new ActivateCompliancePolicy.Command(policy.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        _policyRepository.DidNotReceive().Update(Arg.Any<CompliancePolicy>());
    }

    [Fact]
    public async Task ActivateCompliancePolicy_PolicyNotFound_ShouldReturnError()
    {
        _policyRepository.GetByIdAsync(Arg.Any<CompliancePolicyId>(), Arg.Any<CancellationToken>())
            .Returns((CompliancePolicy?)null);

        var handler = new ActivateCompliancePolicy.Handler(_policyRepository, _clock, _unitOfWork);
        var result = await handler.Handle(new ActivateCompliancePolicy.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // DeactivateCompliancePolicy
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeactivateCompliancePolicy_WhenActive_ShouldDeactivate()
    {
        var policy = MakePolicy(isActive: true);

        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new DeactivateCompliancePolicy.Handler(_policyRepository, _clock, _unitOfWork);
        var result = await handler.Handle(new DeactivateCompliancePolicy.Command(policy.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        _policyRepository.Received(1).Update(policy);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateCompliancePolicy_WhenAlreadyInactive_ShouldBeIdempotent()
    {
        var policy = MakePolicy(isActive: false);

        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>())
            .Returns(policy);

        var handler = new DeactivateCompliancePolicy.Handler(_policyRepository, _clock, _unitOfWork);
        var result = await handler.Handle(new DeactivateCompliancePolicy.Command(policy.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        _policyRepository.DidNotReceive().Update(Arg.Any<CompliancePolicy>());
    }

    // ══════════════════════════════════════════════════════════════
    // TransitionAuditCampaign
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task TransitionAuditCampaign_Start_ShouldTransitionToInProgress()
    {
        var campaign = MakeCampaign(CampaignStatus.Planned);

        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new TransitionAuditCampaign.Handler(_campaignRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new TransitionAuditCampaign.Command(campaign.Id.Value, TransitionAuditCampaign.CampaignAction.Start),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CampaignStatus.InProgress);
        result.Value.StartedAt.Should().Be(FixedNow);
        _campaignRepository.Received(1).Update(campaign);
    }

    [Fact]
    public async Task TransitionAuditCampaign_Complete_ShouldTransitionToCompleted()
    {
        var campaign = MakeCampaign(CampaignStatus.InProgress);

        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new TransitionAuditCampaign.Handler(_campaignRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new TransitionAuditCampaign.Command(campaign.Id.Value, TransitionAuditCampaign.CampaignAction.Complete),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CampaignStatus.Completed);
        result.Value.CompletedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task TransitionAuditCampaign_Cancel_FromPlanned_ShouldCancel()
    {
        var campaign = MakeCampaign(CampaignStatus.Planned);

        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new TransitionAuditCampaign.Handler(_campaignRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new TransitionAuditCampaign.Command(campaign.Id.Value, TransitionAuditCampaign.CampaignAction.Cancel),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CampaignStatus.Cancelled);
    }

    [Fact]
    public async Task TransitionAuditCampaign_Cancel_FromInProgress_ShouldCancel()
    {
        var campaign = MakeCampaign(CampaignStatus.InProgress);

        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new TransitionAuditCampaign.Handler(_campaignRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new TransitionAuditCampaign.Command(campaign.Id.Value, TransitionAuditCampaign.CampaignAction.Cancel),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CampaignStatus.Cancelled);
    }

    [Fact]
    public async Task TransitionAuditCampaign_Start_WhenAlreadyInProgress_ShouldReturnConflictError()
    {
        var campaign = MakeCampaign(CampaignStatus.InProgress);

        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new TransitionAuditCampaign.Handler(_campaignRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new TransitionAuditCampaign.Command(campaign.Id.Value, TransitionAuditCampaign.CampaignAction.Start),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _campaignRepository.DidNotReceive().Update(Arg.Any<AuditCampaign>());
    }

    [Fact]
    public async Task TransitionAuditCampaign_Complete_WhenCompleted_ShouldReturnConflictError()
    {
        var campaign = MakeCampaign(CampaignStatus.Completed);

        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new TransitionAuditCampaign.Handler(_campaignRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new TransitionAuditCampaign.Command(campaign.Id.Value, TransitionAuditCampaign.CampaignAction.Complete),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task TransitionAuditCampaign_CampaignNotFound_ShouldReturnError()
    {
        _campaignRepository.GetByIdAsync(Arg.Any<AuditCampaignId>(), Arg.Any<CancellationToken>())
            .Returns((AuditCampaign?)null);

        var handler = new TransitionAuditCampaign.Handler(_campaignRepository, _clock, _unitOfWork);
        var result = await handler.Handle(
            new TransitionAuditCampaign.Command(Guid.NewGuid(), TransitionAuditCampaign.CampaignAction.Start),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void TransitionAuditCampaignValidator_EmptyId_ShouldFail()
    {
        var v = new TransitionAuditCampaign.Validator();
        v.Validate(new TransitionAuditCampaign.Command(Guid.Empty, TransitionAuditCampaign.CampaignAction.Start))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void TransitionAuditCampaignValidator_ValidCommand_ShouldPass()
    {
        var v = new TransitionAuditCampaign.Validator();
        v.Validate(new TransitionAuditCampaign.Command(Guid.NewGuid(), TransitionAuditCampaign.CampaignAction.Complete))
            .IsValid.Should().BeTrue();
    }
}
