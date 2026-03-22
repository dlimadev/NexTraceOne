using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;

namespace NexTraceOne.AuditCompliance.Tests.Domain.Entities;

/// <summary>
/// Testes de unidade para as entidades de compliance adicionadas na Phase 3.
/// Cobre CompliancePolicy, AuditCampaign e ComplianceResult.
/// </summary>
public sealed class Phase3ComplianceDomainTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── CompliancePolicy ──

    [Fact]
    public void CompliancePolicy_Create_ShouldSetAllProperties()
    {
        var policy = CompliancePolicy.Create(
            "sec-01", "Security Baseline", "Minimum security controls",
            "Security", ComplianceSeverity.High, "{\"check\":\"firewall\"}", _tenantId, _now);

        policy.Should().NotBeNull();
        policy.Id.Value.Should().NotBeEmpty();
        policy.Name.Should().Be("sec-01");
        policy.DisplayName.Should().Be("Security Baseline");
        policy.Description.Should().Be("Minimum security controls");
        policy.Category.Should().Be("Security");
        policy.Severity.Should().Be(ComplianceSeverity.High);
        policy.IsActive.Should().BeTrue();
        policy.EvaluationCriteria.Should().Be("{\"check\":\"firewall\"}");
        policy.TenantId.Should().Be(_tenantId);
        policy.CreatedAt.Should().Be(_now);
        policy.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void CompliancePolicy_Update_ShouldModifyMutableFields()
    {
        var policy = CompliancePolicy.Create("p1", "Policy 1", null, "Operational",
            ComplianceSeverity.Low, null, _tenantId, _now);

        var later = _now.AddHours(1);
        policy.Update("Policy 1 Updated", "New description", "Security",
            ComplianceSeverity.Critical, "{\"rule\":\"updated\"}", later);

        policy.DisplayName.Should().Be("Policy 1 Updated");
        policy.Description.Should().Be("New description");
        policy.Category.Should().Be("Security");
        policy.Severity.Should().Be(ComplianceSeverity.Critical);
        policy.EvaluationCriteria.Should().Be("{\"rule\":\"updated\"}");
        policy.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void CompliancePolicy_Deactivate_ShouldSetIsActiveFalse()
    {
        var policy = CompliancePolicy.Create("p1", "Policy 1", null, "Security",
            ComplianceSeverity.Medium, null, _tenantId, _now);

        policy.Deactivate(_now.AddHours(1));

        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CompliancePolicy_Activate_ShouldSetIsActiveTrue()
    {
        var policy = CompliancePolicy.Create("p1", "Policy 1", null, "Security",
            ComplianceSeverity.Medium, null, _tenantId, _now);
        policy.Deactivate(_now.AddMinutes(1));

        policy.Activate(_now.AddHours(1));

        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CompliancePolicy_Create_NullName_ShouldThrow()
    {
        var act = () => CompliancePolicy.Create(
            null!, "Display", null, "Security",
            ComplianceSeverity.Low, null, _tenantId, _now);

        act.Should().Throw<ArgumentException>();
    }

    // ── AuditCampaign ──

    [Fact]
    public void AuditCampaign_Create_ShouldSetStatusPlanned()
    {
        var campaign = AuditCampaign.Create("Q1 Audit", "Quarterly audit",
            "Periodic", _now.AddDays(7), _tenantId, "admin@test.com", _now);

        campaign.Should().NotBeNull();
        campaign.Id.Value.Should().NotBeEmpty();
        campaign.Name.Should().Be("Q1 Audit");
        campaign.CampaignType.Should().Be("Periodic");
        campaign.Status.Should().Be(CampaignStatus.Planned);
        campaign.ScheduledStartAt.Should().Be(_now.AddDays(7));
        campaign.StartedAt.Should().BeNull();
        campaign.CompletedAt.Should().BeNull();
        campaign.CreatedBy.Should().Be("admin@test.com");
    }

    [Fact]
    public void AuditCampaign_Start_ShouldSetStatusInProgress()
    {
        var campaign = AuditCampaign.Create("Audit", null, "AdHoc", null,
            _tenantId, "admin@test.com", _now);

        campaign.Start(_now.AddHours(1));

        campaign.Status.Should().Be(CampaignStatus.InProgress);
        campaign.StartedAt.Should().Be(_now.AddHours(1));
    }

    [Fact]
    public void AuditCampaign_Complete_ShouldSetStatusCompleted()
    {
        var campaign = AuditCampaign.Create("Audit", null, "AdHoc", null,
            _tenantId, "admin@test.com", _now);
        campaign.Start(_now.AddHours(1));

        campaign.Complete(_now.AddHours(2));

        campaign.Status.Should().Be(CampaignStatus.Completed);
        campaign.CompletedAt.Should().Be(_now.AddHours(2));
    }

    [Fact]
    public void AuditCampaign_Cancel_ShouldSetStatusCancelled()
    {
        var campaign = AuditCampaign.Create("Audit", null, "Regulatory", null,
            _tenantId, "admin@test.com", _now);

        campaign.Cancel(_now.AddHours(1));

        campaign.Status.Should().Be(CampaignStatus.Cancelled);
    }

    [Fact]
    public void AuditCampaign_Start_WhenNotPlanned_ShouldThrow()
    {
        var campaign = AuditCampaign.Create("Audit", null, "AdHoc", null,
            _tenantId, "admin@test.com", _now);
        campaign.Start(_now.AddHours(1));
        campaign.Complete(_now.AddHours(2));

        var act = () => campaign.Start(_now.AddHours(3));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AuditCampaign_Complete_WhenNotInProgress_ShouldThrow()
    {
        var campaign = AuditCampaign.Create("Audit", null, "AdHoc", null,
            _tenantId, "admin@test.com", _now);

        var act = () => campaign.Complete(_now.AddHours(1));

        act.Should().Throw<InvalidOperationException>();
    }

    // ── ComplianceResult ──

    [Fact]
    public void ComplianceResult_Create_ShouldSetAllRequiredFields()
    {
        var policyId = CompliancePolicyId.New();
        var campaignId = AuditCampaignId.New();

        var result = ComplianceResult.Create(
            policyId, campaignId, "Service", "svc-001",
            ComplianceOutcome.Compliant, "{\"passed\":true}",
            "auditor@test.com", _now, _tenantId);

        result.Should().NotBeNull();
        result.Id.Value.Should().NotBeEmpty();
        result.PolicyId.Should().Be(policyId);
        result.CampaignId.Should().Be(campaignId);
        result.ResourceType.Should().Be("Service");
        result.ResourceId.Should().Be("svc-001");
        result.Outcome.Should().Be(ComplianceOutcome.Compliant);
        result.Details.Should().Be("{\"passed\":true}");
        result.EvaluatedBy.Should().Be("auditor@test.com");
        result.EvaluatedAt.Should().Be(_now);
        result.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void ComplianceResult_Create_WithoutCampaign_ShouldAllowNullCampaignId()
    {
        var policyId = CompliancePolicyId.New();

        var result = ComplianceResult.Create(
            policyId, null, "API", "api-002",
            ComplianceOutcome.NonCompliant, null,
            "auditor@test.com", _now, _tenantId);

        result.CampaignId.Should().BeNull();
        result.Outcome.Should().Be(ComplianceOutcome.NonCompliant);
    }
}
