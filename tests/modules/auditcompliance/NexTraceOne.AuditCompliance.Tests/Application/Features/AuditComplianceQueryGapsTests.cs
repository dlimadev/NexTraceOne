using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.CreateCompliancePolicy;
using NexTraceOne.AuditCompliance.Application.Features.GetAuditCampaign;
using NexTraceOne.AuditCompliance.Application.Features.GetCompliancePolicy;
using NexTraceOne.AuditCompliance.Application.Features.ListAuditCampaigns;
using NexTraceOne.AuditCompliance.Application.Features.ListCompliancePolicies;
using NexTraceOne.AuditCompliance.Application.Features.ListComplianceResults;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes para as features de consulta sem cobertura prévia:
/// GetAuditCampaign, GetCompliancePolicy, ListAuditCampaigns,
/// ListCompliancePolicies, ListComplianceResults e CreateCompliancePolicy.
/// </summary>
public sealed class AuditComplianceQueryGapsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static AuditCampaign MakeCampaign(CampaignStatus status = CampaignStatus.Planned)
    {
        var c = AuditCampaign.Create(
            "Quarterly Security Review", "Review all services",
            "Periodic", null,
            TenantId, "admin@nextraceone.local", FixedNow);
        if (status == CampaignStatus.InProgress)
            c.Start(FixedNow);
        return c;
    }

    private static CompliancePolicy MakePolicy(bool active = true)
    {
        var p = CompliancePolicy.Create(
            "mfa-required", "MFA Required", "Multi-factor auth must be enabled",
            "Security", ComplianceSeverity.High, "Check SSO provider config",
            TenantId, FixedNow);
        if (!active)
            p.Deactivate(FixedNow);
        return p;
    }

    private static ComplianceResult MakeResult(CompliancePolicyId policyId, ComplianceOutcome outcome)
        => ComplianceResult.Create(
            policyId, null,
            "Service", "order-service",
            outcome, "Automated check",
            "compliance-scanner", FixedNow,
            TenantId);

    // ═══════════════════════════════════════════════════════════════════
    // GetAuditCampaign
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAuditCampaign_Should_ReturnCampaign_When_Exists()
    {
        var campaign = MakeCampaign();
        var repo = Substitute.For<IAuditCampaignRepository>();
        repo.GetByIdAsync(Arg.Any<AuditCampaignId>(), Arg.Any<CancellationToken>()).Returns(campaign);

        var handler = new GetAuditCampaign.Handler(repo);
        var result = await handler.Handle(new GetAuditCampaign.Query(campaign.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CampaignId.Should().Be(campaign.Id.Value);
        result.Value.Name.Should().Be("Quarterly Security Review");
        result.Value.Status.Should().Be(CampaignStatus.Planned);
    }

    [Fact]
    public async Task GetAuditCampaign_Should_ReturnError_When_NotFound()
    {
        var repo = Substitute.For<IAuditCampaignRepository>();
        repo.GetByIdAsync(Arg.Any<AuditCampaignId>(), Arg.Any<CancellationToken>())
            .Returns((AuditCampaign?)null);

        var handler = new GetAuditCampaign.Handler(repo);
        var result = await handler.Handle(new GetAuditCampaign.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Audit.Campaign.NotFound");
    }

    [Fact]
    public void GetAuditCampaign_Validator_Should_Reject_EmptyId()
    {
        var validator = new GetAuditCampaign.Validator();
        var result = validator.Validate(new GetAuditCampaign.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetCompliancePolicy
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCompliancePolicy_Should_ReturnPolicy_When_Exists()
    {
        var policy = MakePolicy();
        var repo = Substitute.For<ICompliancePolicyRepository>();
        repo.GetByIdAsync(Arg.Any<CompliancePolicyId>(), Arg.Any<CancellationToken>()).Returns(policy);

        var handler = new GetCompliancePolicy.Handler(repo);
        var result = await handler.Handle(new GetCompliancePolicy.Query(policy.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PolicyId.Should().Be(policy.Id.Value);
        result.Value.Name.Should().Be("mfa-required");
        result.Value.Severity.Should().Be(ComplianceSeverity.High);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCompliancePolicy_Should_ReturnError_When_NotFound()
    {
        var repo = Substitute.For<ICompliancePolicyRepository>();
        repo.GetByIdAsync(Arg.Any<CompliancePolicyId>(), Arg.Any<CancellationToken>())
            .Returns((CompliancePolicy?)null);

        var handler = new GetCompliancePolicy.Handler(repo);
        var result = await handler.Handle(new GetCompliancePolicy.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Audit.CompliancePolicy.NotFound");
    }

    [Fact]
    public void GetCompliancePolicy_Validator_Should_Reject_EmptyId()
    {
        var validator = new GetCompliancePolicy.Validator();
        var result = validator.Validate(new GetCompliancePolicy.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ListAuditCampaigns
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListAuditCampaigns_Should_ReturnAll_When_NoFilter()
    {
        var campaigns = new List<AuditCampaign> { MakeCampaign(), MakeCampaign(CampaignStatus.InProgress) };
        var repo = Substitute.For<IAuditCampaignRepository>();
        repo.ListAsync(null, Arg.Any<CancellationToken>()).Returns((IReadOnlyList<AuditCampaign>)campaigns);

        var handler = new ListAuditCampaigns.Handler(repo);
        var result = await handler.Handle(new ListAuditCampaigns.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAuditCampaigns_Should_ReturnFiltered_ByStatus()
    {
        var inProgress = MakeCampaign(CampaignStatus.InProgress);
        var repo = Substitute.For<IAuditCampaignRepository>();
        repo.ListAsync(CampaignStatus.InProgress, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AuditCampaign>)new List<AuditCampaign> { inProgress });

        var handler = new ListAuditCampaigns.Handler(repo);
        var result = await handler.Handle(new ListAuditCampaigns.Query(CampaignStatus.InProgress), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be(CampaignStatus.InProgress);
    }

    [Fact]
    public async Task ListAuditCampaigns_Should_ReturnEmpty_When_None()
    {
        var repo = Substitute.For<IAuditCampaignRepository>();
        repo.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AuditCampaign>)new List<AuditCampaign>());

        var handler = new ListAuditCampaigns.Handler(repo);
        var result = await handler.Handle(new ListAuditCampaigns.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ListCompliancePolicies
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListCompliancePolicies_Should_ReturnAll_When_NoFilter()
    {
        var policies = new List<CompliancePolicy> { MakePolicy(), MakePolicy(active: false) };
        var repo = Substitute.For<ICompliancePolicyRepository>();
        repo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CompliancePolicy>)policies);

        var handler = new ListCompliancePolicies.Handler(repo);
        var result = await handler.Handle(new ListCompliancePolicies.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListCompliancePolicies_Should_ReturnOnlyActive_When_FilterIsActive()
    {
        var active = MakePolicy();
        var repo = Substitute.For<ICompliancePolicyRepository>();
        repo.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CompliancePolicy>)new List<CompliancePolicy> { active });

        var handler = new ListCompliancePolicies.Handler(repo);
        var result = await handler.Handle(new ListCompliancePolicies.Query(true, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ListComplianceResults
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListComplianceResults_Should_ReturnAll_When_NoFilter()
    {
        var policy = MakePolicy();
        var results = new List<ComplianceResult>
        {
            MakeResult(policy.Id, ComplianceOutcome.Compliant),
            MakeResult(policy.Id, ComplianceOutcome.NonCompliant)
        };

        var repo = Substitute.For<IComplianceResultRepository>();
        repo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ComplianceResult>)results);

        var handler = new ListComplianceResults.Handler(repo);
        var result = await handler.Handle(new ListComplianceResults.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListComplianceResults_Should_FilterByOutcome()
    {
        var policy = MakePolicy();
        var nonCompliant = MakeResult(policy.Id, ComplianceOutcome.NonCompliant);
        var repo = Substitute.For<IComplianceResultRepository>();
        repo.ListAsync(null, null, ComplianceOutcome.NonCompliant, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ComplianceResult>)new List<ComplianceResult> { nonCompliant });

        var handler = new ListComplianceResults.Handler(repo);
        var result = await handler.Handle(new ListComplianceResults.Query(null, null, ComplianceOutcome.NonCompliant), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Outcome.Should().Be(ComplianceOutcome.NonCompliant);
    }

    // ═══════════════════════════════════════════════════════════════════
    // CreateCompliancePolicy
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateCompliancePolicy_Should_Create_And_ReturnResponse()
    {
        var repo = Substitute.For<ICompliancePolicyRepository>();
        var uow = Substitute.For<IAuditComplianceUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CreateCompliancePolicy.Handler(repo, CreateClock(), uow);
        var command = new CreateCompliancePolicy.Command(
            "api-version-check", "API Version Must Be Pinned", null,
            "Governance", ComplianceSeverity.Medium, null, TenantId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("api-version-check");
        result.Value.IsActive.Should().BeTrue();
        repo.Received(1).Add(Arg.Any<CompliancePolicy>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateCompliancePolicy_Validator_Should_Reject_EmptyName()
    {
        var validator = new CreateCompliancePolicy.Validator();
        var result = validator.Validate(new CreateCompliancePolicy.Command(
            "", "Display Name", null, "Security", ComplianceSeverity.High, null, TenantId));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCompliancePolicy_Validator_Should_Reject_EmptyTenantId()
    {
        var validator = new CreateCompliancePolicy.Validator();
        var result = validator.Validate(new CreateCompliancePolicy.Command(
            "policy-name", "Display", null, "Security", ComplianceSeverity.High, null, Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCompliancePolicy_Validator_Should_Accept_ValidCommand()
    {
        var validator = new CreateCompliancePolicy.Validator();
        var result = validator.Validate(new CreateCompliancePolicy.Command(
            "policy-name", "Display Name", null, "Security", ComplianceSeverity.High, null, TenantId));
        result.IsValid.Should().BeTrue();
    }
}
