using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.EvaluateContinuousCompliance;
using NexTraceOne.AuditCompliance.Application.Features.ExportComplianceEvidences;
using NexTraceOne.AuditCompliance.Application.Features.GetComplianceDashboard;
using NexTraceOne.AuditCompliance.Application.Features.GetComplianceFrameworkSummary;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes das features de Compliance as Code (Phase 3.5):
///   - GetComplianceFrameworkSummary: resumo por framework (SOC2, ISO27001, LGPD, GDPR, PCI-DSS)
///   - GetComplianceDashboard: dashboard executivo de compliance contínuo
///   - EvaluateContinuousCompliance: avaliação automática de recursos contra políticas
///   - ExportComplianceEvidences: exportação de pacote de evidências para auditores
/// </summary>
public sealed class ComplianceAsCodeTests
{
    private readonly ICompliancePolicyRepository _policyRepository = Substitute.For<ICompliancePolicyRepository>();
    private readonly IComplianceResultRepository _resultRepository = Substitute.For<IComplianceResultRepository>();
    private readonly IAuditEventRepository _auditEventRepository = Substitute.For<IAuditEventRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAuditComplianceUnitOfWork _unitOfWork = Substitute.For<IAuditComplianceUnitOfWork>();

    private readonly Guid _tenantId = Guid.NewGuid();

    // ── GetComplianceFrameworkSummary ────────────────────────────────────────────

    [Fact]
    public async Task GetComplianceFrameworkSummary_Validator_InvalidFramework_ShouldFail()
    {
        var validator = new GetComplianceFrameworkSummary.Validator();

        var result = await validator.ValidateAsync(
            new GetComplianceFrameworkSummary.Query("UNKNOWN_FRAMEWORK", _tenantId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Framework");
    }

    [Theory]
    [InlineData("SOC2")]
    [InlineData("ISO27001")]
    [InlineData("LGPD")]
    [InlineData("GDPR")]
    [InlineData("PCI-DSS")]
    public async Task GetComplianceFrameworkSummary_Validator_KnownFramework_ShouldPass(string framework)
    {
        var validator = new GetComplianceFrameworkSummary.Validator();

        var result = await validator.ValidateAsync(
            new GetComplianceFrameworkSummary.Query(framework, _tenantId));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetComplianceFrameworkSummary_WithNoPolicies_ShouldReturnPerfectScore()
    {
        _policyRepository.ListAsync(true, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy>().AsReadOnly());

        var handler = new GetComplianceFrameworkSummary.Handler(_policyRepository, _resultRepository);
        var query = new GetComplianceFrameworkSummary.Query("SOC2", _tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Framework.Should().Be("SOC2");
        result.Value.OverallScore.Should().Be(100m);
        result.Value.OverallStatus.Should().Be("Green");
        result.Value.TotalControls.Should().Be(0);
        result.Value.CategoryBreakdowns.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetComplianceFrameworkSummary_WithCriticalNonCompliant_ShouldReturnRed()
    {
        var policy = CreatePolicy(_tenantId, "Security", ComplianceSeverity.Critical);
        _policyRepository.ListAsync(true, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy> { policy }.AsReadOnly());

        var nonCompliantResult = CreateResult(policy.Id, ComplianceOutcome.NonCompliant, _tenantId);
        _resultRepository.ListAsync(policy.Id, null, null, CancellationToken.None)
            .Returns(new List<ComplianceResult> { nonCompliantResult }.AsReadOnly());

        var handler = new GetComplianceFrameworkSummary.Handler(_policyRepository, _resultRepository);
        var query = new GetComplianceFrameworkSummary.Query("SOC2", _tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("Red");
        result.Value.NonCompliant.Should().BeGreaterThan(0);
    }

    // ── GetComplianceDashboard ───────────────────────────────────────────────────

    [Fact]
    public async Task GetComplianceDashboard_WithNoPolicies_ShouldReturnGreen()
    {
        _policyRepository.ListAsync(true, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy>().AsReadOnly());
        _resultRepository.ListAsync(null, null, null, CancellationToken.None)
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = new GetComplianceDashboard.Handler(_policyRepository, _resultRepository);
        var query = new GetComplianceDashboard.Query(_tenantId, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.OverallStatus.Should().Be("Green");
        result.Value.OverallScore.Should().Be(100m);
        result.Value.TotalPolicies.Should().Be(0);
        result.Value.CriticalGaps.Should().BeEmpty();
    }

    [Fact]
    public async Task GetComplianceDashboard_WithNonCompliantPolicies_ShouldHaveCriticalGaps()
    {
        var criticalPolicy = CreatePolicy(_tenantId, "Security", ComplianceSeverity.Critical);
        _policyRepository.ListAsync(true, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy> { criticalPolicy }.AsReadOnly());

        var nonCompliantResult = CreateResult(criticalPolicy.Id, ComplianceOutcome.NonCompliant, _tenantId);
        _resultRepository.ListAsync(null, null, null, CancellationToken.None)
            .Returns(new List<ComplianceResult> { nonCompliantResult }.AsReadOnly());

        var handler = new GetComplianceDashboard.Handler(_policyRepository, _resultRepository);
        var query = new GetComplianceDashboard.Query(_tenantId, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CriticalGaps.Should().NotBeEmpty();
        result.Value.CriticalGaps.Should().Contain(g => g.PolicyId == criticalPolicy.Id.Value);
    }

    [Fact]
    public async Task GetComplianceDashboard_CategoryBreakdown_ShouldGroupByCategory()
    {
        var securityPolicy = CreatePolicy(_tenantId, "Security", ComplianceSeverity.High);
        var dataPolicy = CreatePolicy(_tenantId, "DataProtection", ComplianceSeverity.Medium);
        _policyRepository.ListAsync(true, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy> { securityPolicy, dataPolicy }.AsReadOnly());

        _resultRepository.ListAsync(null, null, null, CancellationToken.None)
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = new GetComplianceDashboard.Handler(_policyRepository, _resultRepository);
        var query = new GetComplianceDashboard.Query(_tenantId, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryBreakdown.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Value.TotalUnevaluated.Should().Be(2);
    }

    // ── EvaluateContinuousCompliance ─────────────────────────────────────────────

    [Fact]
    public async Task EvaluateContinuousCompliance_Validator_MissingResourceType_ShouldFail()
    {
        var validator = new EvaluateContinuousCompliance.Validator();

        var result = await validator.ValidateAsync(
            new EvaluateContinuousCompliance.Command(string.Empty, "res-1", null, _tenantId, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateContinuousCompliance_WithNoPolicies_ShouldReturnZeroEvaluations()
    {
        _policyRepository.ListAsync(true, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy>().AsReadOnly());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new EvaluateContinuousCompliance.Handler(
            _policyRepository, _resultRepository, _dateTimeProvider, _unitOfWork);
        var command = new EvaluateContinuousCompliance.Command(
            "Service", "svc-1", null, _tenantId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PoliciesEvaluated.Should().Be(0);
        result.Value.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateContinuousCompliance_WithActivePolicies_ShouldEvaluateAndPersist()
    {
        var policy = CreatePolicy(_tenantId, "Security", ComplianceSeverity.High,
            evaluationCriteria: "AUDIT");
        _policyRepository.ListAsync(true, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy> { policy }.AsReadOnly());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var handler = new EvaluateContinuousCompliance.Handler(
            _policyRepository, _resultRepository, _dateTimeProvider, _unitOfWork);
        var command = new EvaluateContinuousCompliance.Command(
            "Service", "svc-1", null, _tenantId, "deploy:xyz");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PoliciesEvaluated.Should().Be(1);
        result.Value.Results.Should().HaveCount(1);
        _resultRepository.Received(1).Add(Arg.Any<ComplianceResult>());
        await _unitOfWork.Received(1).CommitAsync(CancellationToken.None);
    }

    // ── ExportComplianceEvidences ────────────────────────────────────────────────

    [Fact]
    public async Task ExportComplianceEvidences_Validator_EmptyTenantId_ShouldFail()
    {
        var validator = new ExportComplianceEvidences.Validator();

        var result = await validator.ValidateAsync(
            new ExportComplianceEvidences.Query(Guid.Empty, null, null, null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ExportComplianceEvidences_WithNoResults_ShouldReturnEmptyPack()
    {
        _policyRepository.ListAsync(null, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy>().AsReadOnly());
        _resultRepository.ListAsync(null, null, null, CancellationToken.None)
            .Returns(new List<ComplianceResult>().AsReadOnly());

        var handler = new ExportComplianceEvidences.Handler(
            _policyRepository, _resultRepository, _auditEventRepository);
        var query = new ExportComplianceEvidences.Query(_tenantId, null, null, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(_tenantId);
        result.Value.TotalEvidences.Should().Be(0);
        result.Value.ExportRef.Should().StartWith("AUDIT-");
        result.Value.Evidences.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportComplianceEvidences_ShouldFilterByFramework()
    {
        var soc2Policy = CreatePolicy(_tenantId, "Security", ComplianceSeverity.High);
        var gdprPolicy = CreatePolicy(_tenantId, "DataProtection", ComplianceSeverity.Medium);

        _policyRepository.ListAsync(null, null, CancellationToken.None)
            .Returns(new List<CompliancePolicy> { soc2Policy, gdprPolicy }.AsReadOnly());

        var soc2Result = CreateResult(soc2Policy.Id, ComplianceOutcome.Compliant, _tenantId);
        _resultRepository.ListAsync(null, null, null, CancellationToken.None)
            .Returns(new List<ComplianceResult> { soc2Result }.AsReadOnly());

        var handler = new ExportComplianceEvidences.Handler(
            _policyRepository, _resultRepository, _auditEventRepository);
        var query = new ExportComplianceEvidences.Query(_tenantId, "SOC2", null, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Framework.Should().Be("SOC2");
        result.Value.Evidences.Should().OnlyContain(e => e.Category.Equals("Security", StringComparison.OrdinalIgnoreCase));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static CompliancePolicy CreatePolicy(
        Guid tenantId,
        string category,
        ComplianceSeverity severity,
        string? evaluationCriteria = null)
    {
        var policy = CompliancePolicy.Create(
            $"POL-{category}-{Guid.NewGuid().ToString("N")[..6]}",
            $"{category} Policy",
            $"Policy for {category}",
            category,
            severity,
            evaluationCriteria,
            tenantId,
            DateTimeOffset.UtcNow);
        return policy;
    }

    private static ComplianceResult CreateResult(
        CompliancePolicyId policyId,
        ComplianceOutcome outcome,
        Guid tenantId)
    {
        return ComplianceResult.Create(
            policyId,
            null,
            "Service",
            "svc-1",
            outcome,
            "Automated evaluation.",
            "system:test",
            DateTimeOffset.UtcNow,
            tenantId);
    }
}
