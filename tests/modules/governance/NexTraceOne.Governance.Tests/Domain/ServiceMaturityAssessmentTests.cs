using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade ServiceMaturityAssessment.
/// Valida derivação de nível, reavaliação, contadores e guard clauses.
/// </summary>
public sealed class ServiceMaturityAssessmentTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Factory method: Assess ──

    [Fact]
    public void Assess_AllCriteriaTrue_ShouldReturnLevel5Resilient()
    {
        var assessment = CreateAssessment(allTrue: true);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Resilient);
        assessment.ReassessmentCount.Should().Be(0);
        assessment.LastReassessedAt.Should().BeNull();
    }

    [Fact]
    public void Assess_Level4Criteria_ShouldReturnObserved()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "order-service",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: true,
            approvalWorkflowActive: true,
            telemetryActive: true,
            baselinesEstablished: true,
            alertsConfigured: true,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: "tenant1",
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Observed);
    }

    [Fact]
    public void Assess_Level3Criteria_ShouldReturnGoverned()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "payment-service",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: true,
            approvalWorkflowActive: true,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "admin",
            tenantId: null,
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Governed);
    }

    [Fact]
    public void Assess_Level2Criteria_ShouldReturnDocumented()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "catalog-service",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: "t1",
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Documented);
    }

    [Fact]
    public void Assess_OnlyOwnership_ShouldReturnBasic()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "legacy-api",
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Basic);
    }

    [Fact]
    public void Assess_NothingDefined_ShouldReturnBasic()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "unknown-svc",
            ownershipDefined: false,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Basic);
        assessment.OwnershipDefined.Should().BeFalse();
    }

    // ── DeriveLevel edge cases ──

    [Fact]
    public void Assess_Level2MissingDocumentation_ShouldNotReachLevel2()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "edge-svc",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: false,
            policiesApplied: true,
            approvalWorkflowActive: true,
            telemetryActive: true,
            baselinesEstablished: true,
            alertsConfigured: true,
            runbooksAvailable: true,
            rollbackTested: true,
            chaosValidated: true,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Basic);
    }

    [Fact]
    public void Assess_Level4MissingAlerts_ShouldStayAtLevel3()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "partial-svc",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: true,
            approvalWorkflowActive: true,
            telemetryActive: true,
            baselinesEstablished: true,
            alertsConfigured: false,
            runbooksAvailable: true,
            rollbackTested: true,
            chaosValidated: true,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Governed);
    }

    // ── Reassess ──

    [Fact]
    public void Reassess_ShouldUpdateCriteriaAndLevel()
    {
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "evolving-svc",
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Basic);

        var laterNow = FixedNow.AddDays(30);
        assessment.Reassess(
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            now: laterNow);

        assessment.CurrentLevel.Should().Be(ServiceMaturityLevel.Documented);
        assessment.LastReassessedAt.Should().Be(laterNow);
    }

    [Fact]
    public void Reassess_ShouldIncrementCount()
    {
        var assessment = CreateAssessment(allTrue: false);
        assessment.ReassessmentCount.Should().Be(0);

        assessment.Reassess(
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            now: FixedNow.AddDays(1));

        assessment.ReassessmentCount.Should().Be(1);

        assessment.Reassess(
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            now: FixedNow.AddDays(2));

        assessment.ReassessmentCount.Should().Be(2);
    }

    // ── Guard clauses ──

    [Fact]
    public void Assess_NullServiceName_ShouldThrow()
    {
        var act = () => ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: null!,
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Assess_EmptyServiceName_ShouldThrow()
    {
        var act = () => ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "   ",
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Assess_EmptyAssessedBy_ShouldThrow()
    {
        var act = () => ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "valid-svc",
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "",
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Assess_DefaultServiceId_ShouldThrow()
    {
        var act = () => ServiceMaturityAssessment.Assess(
            serviceId: Guid.Empty,
            serviceName: "valid-svc",
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Properties set correctly ──

    [Fact]
    public void Assess_ShouldSetAllPropertiesCorrectly()
    {
        var serviceId = Guid.NewGuid();
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: serviceId,
            serviceName: "  my-service  ",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "  john.doe  ",
            tenantId: "  tenant-abc  ",
            now: FixedNow);

        assessment.ServiceId.Should().Be(serviceId);
        assessment.ServiceName.Should().Be("my-service");
        assessment.AssessedBy.Should().Be("john.doe");
        assessment.TenantId.Should().Be("tenant-abc");
        assessment.AssessedAt.Should().Be(FixedNow);
        assessment.Id.Value.Should().NotBe(Guid.Empty);
    }

    // ── Helper ──

    private static ServiceMaturityAssessment CreateAssessment(bool allTrue) =>
        ServiceMaturityAssessment.Assess(
            serviceId: Guid.NewGuid(),
            serviceName: "test-service",
            ownershipDefined: allTrue,
            contractsPublished: allTrue,
            documentationExists: allTrue,
            policiesApplied: allTrue,
            approvalWorkflowActive: allTrue,
            telemetryActive: allTrue,
            baselinesEstablished: allTrue,
            alertsConfigured: allTrue,
            runbooksAvailable: allTrue,
            rollbackTested: allTrue,
            chaosValidated: allTrue,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);
}
