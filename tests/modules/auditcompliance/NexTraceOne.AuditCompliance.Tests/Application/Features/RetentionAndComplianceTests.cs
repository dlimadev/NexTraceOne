using Microsoft.Extensions.Logging;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.ApplyRetention;
using NexTraceOne.AuditCompliance.Application.Features.CreateAuditCampaign;
using NexTraceOne.AuditCompliance.Application.Features.GetComplianceDashboard;
using NexTraceOne.AuditCompliance.Application.Features.GetRetentionPolicies;
using NexTraceOne.AuditCompliance.Application.Features.RecordComplianceResult;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para features de retenção e compliance do módulo AuditCompliance.
/// Cobre ApplyRetention, CreateAuditCampaign, GetRetentionPolicies,
/// RecordComplianceResult e GetComplianceDashboard.
/// </summary>
public sealed class RetentionAndComplianceTests
{
    private readonly IRetentionPolicyRepository _retentionPolicyRepository = Substitute.For<IRetentionPolicyRepository>();
    private readonly IAuditEventRepository _auditEventRepository = Substitute.For<IAuditEventRepository>();
    private readonly IAuditCampaignRepository _auditCampaignRepository = Substitute.For<IAuditCampaignRepository>();
    private readonly ICompliancePolicyRepository _compliancePolicyRepository = Substitute.For<ICompliancePolicyRepository>();
    private readonly IComplianceResultRepository _complianceResultRepository = Substitute.For<IComplianceResultRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IAuditComplianceUnitOfWork _unitOfWork = Substitute.For<IAuditComplianceUnitOfWork>();
    private readonly ILogger<ApplyRetention.Handler> _applyRetentionLogger = Substitute.For<ILogger<ApplyRetention.Handler>>();

    private static readonly DateTimeOffset FixedNow = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public RetentionAndComplianceTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── Factory methods ──

    private ApplyRetention.Handler CreateApplyRetentionHandler() =>
        new(_retentionPolicyRepository, _auditEventRepository, _clock, _applyRetentionLogger);

    private CreateAuditCampaign.Handler CreateAuditCampaignHandler() =>
        new(_auditCampaignRepository, _clock, _unitOfWork);

    private GetRetentionPolicies.Handler CreateGetRetentionPoliciesHandler() =>
        new(_retentionPolicyRepository);

    private RecordComplianceResult.Handler CreateRecordComplianceResultHandler() =>
        new(_complianceResultRepository, _clock, _unitOfWork);

    private GetComplianceDashboard.Handler CreateGetComplianceDashboardHandler() =>
        new(_compliancePolicyRepository, _complianceResultRepository);

    // ══════════════════════════════════════════════════════════════
    // ApplyRetention
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApplyRetention_WhenNoActivePolicy_ShouldReturnPolicyAppliedFalse()
    {
        _retentionPolicyRepository.GetMostRestrictiveActiveAsync(Arg.Any<CancellationToken>())
            .Returns((RetentionPolicy?)null);

        var handler = CreateApplyRetentionHandler();
        var result = await handler.Handle(new ApplyRetention.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PolicyApplied.Should().BeFalse();
        result.Value.PolicyName.Should().BeNull();
        result.Value.RetentionDays.Should().Be(0);
        result.Value.DeletedEventCount.Should().Be(0);

        await _auditEventRepository.DidNotReceive()
            .DeleteExpiredAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyRetention_WhenPolicyExists_AndNoExpiredEvents_ShouldReturnZeroDeleted()
    {
        var policy = RetentionPolicy.Create("30-Day Policy", 30);

        _retentionPolicyRepository.GetMostRestrictiveActiveAsync(Arg.Any<CancellationToken>())
            .Returns(policy);
        _auditEventRepository.DeleteExpiredAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = CreateApplyRetentionHandler();
        var result = await handler.Handle(new ApplyRetention.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PolicyApplied.Should().BeTrue();
        result.Value.PolicyName.Should().Be("30-Day Policy");
        result.Value.RetentionDays.Should().Be(30);
        result.Value.Cutoff.Should().Be(FixedNow.AddDays(-30));
        result.Value.DeletedEventCount.Should().Be(0);
    }

    [Fact]
    public async Task ApplyRetention_WhenPolicyExists_ShouldDeleteExpiredEvents()
    {
        var policy = RetentionPolicy.Create("90-Day Retention", 90);
        var expectedCutoff = FixedNow.AddDays(-90);

        _retentionPolicyRepository.GetMostRestrictiveActiveAsync(Arg.Any<CancellationToken>())
            .Returns(policy);
        _auditEventRepository.DeleteExpiredAsync(expectedCutoff, Arg.Any<CancellationToken>())
            .Returns(42);

        var handler = CreateApplyRetentionHandler();
        var result = await handler.Handle(new ApplyRetention.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PolicyApplied.Should().BeTrue();
        result.Value.PolicyName.Should().Be("90-Day Retention");
        result.Value.RetentionDays.Should().Be(90);
        result.Value.Cutoff.Should().Be(expectedCutoff);
        result.Value.DeletedEventCount.Should().Be(42);

        await _auditEventRepository.Received(1)
            .DeleteExpiredAsync(expectedCutoff, Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    // CreateAuditCampaign
    // ══════════════════════════════════════════════════════════════

    private static CreateAuditCampaign.Command CreateValidCampaignCommand() =>
        new("Q2 2025 Audit", "Quarterly audit", "Periodic", FixedNow.AddDays(7), Guid.NewGuid(), "admin@org.com");

    [Fact]
    public async Task CreateAuditCampaign_ValidCommand_ShouldSucceed()
    {
        var command = CreateValidCampaignCommand();

        var handler = CreateAuditCampaignHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CampaignId.Should().NotBeEmpty();
        result.Value.Name.Should().Be("Q2 2025 Audit");
        result.Value.Status.Should().Be(CampaignStatus.Planned.ToString());

        _auditCampaignRepository.Received(1).Add(Arg.Any<AuditCampaign>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAuditCampaign_ShouldPassCorrectTimestampToEntity()
    {
        var command = CreateValidCampaignCommand();

        var handler = CreateAuditCampaignHandler();
        await handler.Handle(command, CancellationToken.None);

        _auditCampaignRepository.Received(1).Add(Arg.Is<AuditCampaign>(c =>
            c.CreatedAt == FixedNow && c.Name == "Q2 2025 Audit"));
    }

    // ── CreateAuditCampaign Validator ──

    [Fact]
    public void CreateAuditCampaign_Validator_ValidCommand_ShouldPass()
    {
        var validator = new CreateAuditCampaign.Validator();
        var result = validator.Validate(CreateValidCampaignCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAuditCampaign_Validator_EmptyName_ShouldFail()
    {
        var validator = new CreateAuditCampaign.Validator();
        var command = new CreateAuditCampaign.Command("", "desc", "Periodic", null, Guid.NewGuid(), "admin");
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateAuditCampaign_Validator_EmptyCampaignType_ShouldFail()
    {
        var validator = new CreateAuditCampaign.Validator();
        var command = new CreateAuditCampaign.Command("Name", "desc", "", null, Guid.NewGuid(), "admin");
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateAuditCampaign_Validator_EmptyTenantId_ShouldFail()
    {
        var validator = new CreateAuditCampaign.Validator();
        var command = new CreateAuditCampaign.Command("Name", "desc", "Periodic", null, Guid.Empty, "admin");
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateAuditCampaign_Validator_EmptyCreatedBy_ShouldFail()
    {
        var validator = new CreateAuditCampaign.Validator();
        var command = new CreateAuditCampaign.Command("Name", "desc", "Periodic", null, Guid.NewGuid(), "");
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateAuditCampaign_Validator_NameTooLong_ShouldFail()
    {
        var validator = new CreateAuditCampaign.Validator();
        var command = new CreateAuditCampaign.Command(new string('A', 201), "desc", "Periodic", null, Guid.NewGuid(), "admin");
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // GetRetentionPolicies
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRetentionPolicies_WhenActiveOnly_ShouldFilterActive()
    {
        var activePolicy = RetentionPolicy.Create("Active Policy", 60);
        _retentionPolicyRepository.ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RetentionPolicy> { activePolicy }.AsReadOnly());

        var handler = CreateGetRetentionPoliciesHandler();
        var result = await handler.Handle(new GetRetentionPolicies.Query(ActiveOnly: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().HaveCount(1);
        result.Value.Policies[0].PolicyName.Should().Be("Active Policy");
        result.Value.Policies[0].RetentionDays.Should().Be(60);
        result.Value.Policies[0].IsActive.Should().BeTrue();

        await _retentionPolicyRepository.Received(1).ListActiveAsync(Arg.Any<CancellationToken>());
        await _retentionPolicyRepository.DidNotReceive().ListAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRetentionPolicies_WhenAll_ShouldReturnAll()
    {
        var policy1 = RetentionPolicy.Create("Active", 30);
        var policy2 = RetentionPolicy.Create("Inactive", 90);
        policy2.Deactivate();

        _retentionPolicyRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RetentionPolicy> { policy1, policy2 }.AsReadOnly());

        var handler = CreateGetRetentionPoliciesHandler();
        var result = await handler.Handle(new GetRetentionPolicies.Query(ActiveOnly: null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().HaveCount(2);

        await _retentionPolicyRepository.DidNotReceive().ListActiveAsync(Arg.Any<CancellationToken>());
        await _retentionPolicyRepository.Received(1).ListAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRetentionPolicies_WhenEmpty_ShouldReturnEmptyList()
    {
        _retentionPolicyRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RetentionPolicy>().AsReadOnly());

        var handler = CreateGetRetentionPoliciesHandler();
        var result = await handler.Handle(new GetRetentionPolicies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════
    // RecordComplianceResult
    // ══════════════════════════════════════════════════════════════

    private static RecordComplianceResult.Command CreateValidComplianceResultCommand() =>
        new(Guid.NewGuid(), null, "Service", "svc-payment", ComplianceOutcome.Compliant, null, "auditor@org.com", Guid.NewGuid());

    [Fact]
    public async Task RecordComplianceResult_ValidCommand_ShouldSucceed()
    {
        var command = CreateValidComplianceResultCommand();

        var handler = CreateRecordComplianceResultHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResultId.Should().NotBeEmpty();
        result.Value.Outcome.Should().Be(ComplianceOutcome.Compliant);
        result.Value.EvaluatedAt.Should().Be(FixedNow);

        _complianceResultRepository.Received(1).Add(Arg.Any<ComplianceResult>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordComplianceResult_WithCampaignId_ShouldPersist()
    {
        var campaignId = Guid.NewGuid();
        var command = new RecordComplianceResult.Command(
            Guid.NewGuid(), campaignId, "API", "api-orders", ComplianceOutcome.NonCompliant,
            """{"reason":"missing auth"}""", "auditor@org.com", Guid.NewGuid());

        var handler = CreateRecordComplianceResultHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be(ComplianceOutcome.NonCompliant);

        _complianceResultRepository.Received(1).Add(Arg.Is<ComplianceResult>(r =>
            r.CampaignId != null && r.CampaignId.Value == campaignId));
    }

    // ── RecordComplianceResult Validator ──

    [Fact]
    public void RecordComplianceResult_Validator_ValidCommand_ShouldPass()
    {
        var validator = new RecordComplianceResult.Validator();
        var result = validator.Validate(CreateValidComplianceResultCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RecordComplianceResult_Validator_EmptyPolicyId_ShouldFail()
    {
        var validator = new RecordComplianceResult.Validator();
        var command = new RecordComplianceResult.Command(
            Guid.Empty, null, "Service", "svc-1", ComplianceOutcome.Compliant, null, "auditor", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordComplianceResult_Validator_EmptyResourceType_ShouldFail()
    {
        var validator = new RecordComplianceResult.Validator();
        var command = new RecordComplianceResult.Command(
            Guid.NewGuid(), null, "", "svc-1", ComplianceOutcome.Compliant, null, "auditor", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordComplianceResult_Validator_EmptyResourceId_ShouldFail()
    {
        var validator = new RecordComplianceResult.Validator();
        var command = new RecordComplianceResult.Command(
            Guid.NewGuid(), null, "Service", "", ComplianceOutcome.Compliant, null, "auditor", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordComplianceResult_Validator_EmptyEvaluatedBy_ShouldFail()
    {
        var validator = new RecordComplianceResult.Validator();
        var command = new RecordComplianceResult.Command(
            Guid.NewGuid(), null, "Service", "svc-1", ComplianceOutcome.Compliant, null, "", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordComplianceResult_Validator_EmptyTenantId_ShouldFail()
    {
        var validator = new RecordComplianceResult.Validator();
        var command = new RecordComplianceResult.Command(
            Guid.NewGuid(), null, "Service", "svc-1", ComplianceOutcome.Compliant, null, "auditor", Guid.Empty);
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    // GetComplianceDashboard
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetComplianceDashboard_EmptyPolicies_ShouldReturnDefaults()
    {
        var tenantId = Guid.NewGuid();

        _compliancePolicyRepository.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<CompliancePolicy>().AsReadOnly());
        _complianceResultRepository.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = CreateGetComplianceDashboardHandler();
        var result = await handler.Handle(
            new GetComplianceDashboard.Query(tenantId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.TotalPolicies.Should().Be(0);
        result.Value.TotalEvaluated.Should().Be(0);
        result.Value.OverallStatus.Should().Be("Green");
        result.Value.OverallScore.Should().Be(100m);
        result.Value.CriticalGaps.Should().BeEmpty();
        result.Value.CategoryBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GetComplianceDashboard_MixedResults_ShouldAggregateCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var securityPolicy = CompliancePolicy.Create(
            "sec-auth", "Auth Policy", null, "Security", ComplianceSeverity.High, null, tenantId, now);
        var dataPolicy = CompliancePolicy.Create(
            "data-enc", "Encryption Policy", null, "DataProtection", ComplianceSeverity.Critical, null, tenantId, now);

        var compliantResult = ComplianceResult.Create(
            securityPolicy.Id, null, "Service", "svc-1", ComplianceOutcome.Compliant,
            null, "auditor", now, tenantId);
        var nonCompliantResult = ComplianceResult.Create(
            dataPolicy.Id, null, "Service", "svc-2", ComplianceOutcome.NonCompliant,
            null, "auditor", now, tenantId);

        _compliancePolicyRepository.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<CompliancePolicy> { securityPolicy, dataPolicy }.AsReadOnly());
        _complianceResultRepository.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult> { compliantResult, nonCompliantResult }.AsReadOnly());

        var handler = CreateGetComplianceDashboardHandler();
        var result = await handler.Handle(
            new GetComplianceDashboard.Query(tenantId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPolicies.Should().Be(2);
        result.Value.TotalEvaluated.Should().Be(2);
        result.Value.Compliant.Should().Be(1);
        result.Value.NonCompliant.Should().Be(1);
        result.Value.CategoryBreakdown.Should().HaveCount(2);
        result.Value.CriticalGaps.Should().HaveCount(1);
        result.Value.CriticalGaps[0].PolicyName.Should().Be("Encryption Policy");
    }

    [Fact]
    public async Task GetComplianceDashboard_AllCompliant_ShouldReturnGreenStatus()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var policy = CompliancePolicy.Create(
            "pol-1", "Policy 1", null, "Security", ComplianceSeverity.High, null, tenantId, now);
        var compliantResult = ComplianceResult.Create(
            policy.Id, null, "Service", "svc-1", ComplianceOutcome.Compliant,
            null, "auditor", now, tenantId);

        _compliancePolicyRepository.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<CompliancePolicy> { policy }.AsReadOnly());
        _complianceResultRepository.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ComplianceResult> { compliantResult }.AsReadOnly());

        var handler = CreateGetComplianceDashboardHandler();
        var result = await handler.Handle(
            new GetComplianceDashboard.Query(tenantId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("Green");
        result.Value.OverallScore.Should().Be(100m);
        result.Value.NonCompliant.Should().Be(0);
        result.Value.CriticalGaps.Should().BeEmpty();
    }
}
