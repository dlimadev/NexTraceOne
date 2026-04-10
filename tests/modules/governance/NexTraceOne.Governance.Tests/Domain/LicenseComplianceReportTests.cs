using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade LicenseComplianceReport.
/// Valida factory Generate, cálculo de compliance percent, guard clauses, scopes, risk levels e defaults.
/// </summary>
public sealed class LicenseComplianceReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    // ── Factory method: Generate — valid scenarios ──

    [Fact]
    public void Generate_ValidInputs_ShouldCreateReport()
    {
        var report = CreateValid();

        report.Id.Value.Should().NotBe(Guid.Empty);
        report.Scope.Should().Be(LicenseComplianceScope.Service);
        report.ScopeKey.Should().Be("payment-service");
        report.ScopeLabel.Should().Be("Payment Service");
        report.TotalDependencies.Should().Be(50);
        report.CompliantCount.Should().Be(45);
        report.NonCompliantCount.Should().Be(3);
        report.WarningCount.Should().Be(2);
        report.OverallRiskLevel.Should().Be(LicenseRiskLevel.Medium);
        report.CompliancePercent.Should().Be(90);
        report.LicenseDetails.Should().NotBeNull();
        report.Conflicts.Should().NotBeNull();
        report.Recommendations.Should().NotBeNull();
        report.ScannedAt.Should().Be(FixedNow);
        report.TenantId.Should().Be("tenant1");
    }

    [Fact]
    public void Generate_AllScopes_ShouldBeAccepted()
    {
        foreach (var scope in Enum.GetValues<LicenseComplianceScope>())
        {
            var report = LicenseComplianceReport.Generate(
                scope: scope,
                scopeKey: "test-key",
                scopeLabel: null,
                totalDependencies: 10,
                compliantCount: 8,
                nonCompliantCount: 1,
                warningCount: 1,
                overallRiskLevel: LicenseRiskLevel.Low,
                licenseDetails: null,
                conflicts: null,
                recommendations: null,
                tenantId: null,
                now: FixedNow);

            report.Scope.Should().Be(scope);
        }
    }

    [Fact]
    public void Generate_AllRiskLevels_ShouldBeAccepted()
    {
        foreach (var riskLevel in Enum.GetValues<LicenseRiskLevel>())
        {
            var report = LicenseComplianceReport.Generate(
                scope: LicenseComplianceScope.Service,
                scopeKey: "test-svc",
                scopeLabel: null,
                totalDependencies: 10,
                compliantCount: 5,
                nonCompliantCount: 3,
                warningCount: 2,
                overallRiskLevel: riskLevel,
                licenseDetails: null,
                conflicts: null,
                recommendations: null,
                tenantId: null,
                now: FixedNow);

            report.OverallRiskLevel.Should().Be(riskLevel);
        }
    }

    [Fact]
    public void Generate_CompliancePercent_ShouldBeCalculatedCorrectly()
    {
        var report = LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Team,
            scopeKey: "platform-team",
            scopeLabel: null,
            totalDependencies: 200,
            compliantCount: 150,
            nonCompliantCount: 30,
            warningCount: 20,
            overallRiskLevel: LicenseRiskLevel.Medium,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        report.CompliancePercent.Should().Be(75);
    }

    [Fact]
    public void Generate_ZeroDependencies_ShouldGive100PercentCompliance()
    {
        var report = LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Domain,
            scopeKey: "empty-domain",
            scopeLabel: "Empty Domain",
            totalDependencies: 0,
            compliantCount: 0,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        report.CompliancePercent.Should().Be(100);
        report.TotalDependencies.Should().Be(0);
    }

    [Fact]
    public void Generate_FullCompliance_ShouldBe100Percent()
    {
        var report = LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "clean-svc",
            scopeLabel: null,
            totalDependencies: 25,
            compliantCount: 25,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        report.CompliancePercent.Should().Be(100);
    }

    [Fact]
    public void Generate_ZeroCompliant_ShouldBe0Percent()
    {
        var report = LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "risky-svc",
            scopeLabel: null,
            totalDependencies: 10,
            compliantCount: 0,
            nonCompliantCount: 10,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Critical,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        report.CompliancePercent.Should().Be(0);
    }

    [Fact]
    public void Generate_NullOptionalFields_ShouldBeValid()
    {
        var report = LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Team,
            scopeKey: "minimal-team",
            scopeLabel: null,
            totalDependencies: 5,
            compliantCount: 5,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        report.ScopeLabel.Should().BeNull();
        report.LicenseDetails.Should().BeNull();
        report.Conflicts.Should().BeNull();
        report.Recommendations.Should().BeNull();
        report.TenantId.Should().BeNull();
    }

    [Fact]
    public void Generate_TrimsStrings()
    {
        var report = LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "  svc-name  ",
            scopeLabel: "  Service Label  ",
            totalDependencies: 10,
            compliantCount: 8,
            nonCompliantCount: 1,
            warningCount: 1,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: "  t1  ",
            now: FixedNow);

        report.ScopeKey.Should().Be("svc-name");
        report.ScopeLabel.Should().Be("Service Label");
        report.TenantId.Should().Be("t1");
    }

    [Fact]
    public void Generate_TypedId_ShouldBeUnique()
    {
        var report1 = CreateValid();
        var report2 = CreateValid();

        report1.Id.Should().NotBe(report2.Id);
        report1.Id.Value.Should().NotBe(report2.Id.Value);
    }

    // ── Guard clauses ──

    [Fact]
    public void Generate_NullScopeKey_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: null!,
            scopeLabel: null,
            totalDependencies: 0,
            compliantCount: 0,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_EmptyScopeKey_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "   ",
            scopeLabel: null,
            totalDependencies: 0,
            compliantCount: 0,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_ScopeKeyTooLong_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: new string('x', 201),
            scopeLabel: null,
            totalDependencies: 0,
            compliantCount: 0,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_ScopeLabelTooLong_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "svc",
            scopeLabel: new string('x', 301),
            totalDependencies: 0,
            compliantCount: 0,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_NegativeTotalDependencies_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "svc",
            scopeLabel: null,
            totalDependencies: -1,
            compliantCount: 0,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_NegativeCompliantCount_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "svc",
            scopeLabel: null,
            totalDependencies: 10,
            compliantCount: -1,
            nonCompliantCount: 0,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_NegativeNonCompliantCount_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "svc",
            scopeLabel: null,
            totalDependencies: 10,
            compliantCount: 5,
            nonCompliantCount: -1,
            warningCount: 0,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_NegativeWarningCount_ShouldThrow()
    {
        var act = () => LicenseComplianceReport.Generate(
            scope: LicenseComplianceScope.Service,
            scopeKey: "svc",
            scopeLabel: null,
            totalDependencies: 10,
            compliantCount: 5,
            nonCompliantCount: 3,
            warningCount: -1,
            overallRiskLevel: LicenseRiskLevel.Low,
            licenseDetails: null,
            conflicts: null,
            recommendations: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Helper ──

    private static LicenseComplianceReport CreateValid() => LicenseComplianceReport.Generate(
        scope: LicenseComplianceScope.Service,
        scopeKey: "payment-service",
        scopeLabel: "Payment Service",
        totalDependencies: 50,
        compliantCount: 45,
        nonCompliantCount: 3,
        warningCount: 2,
        overallRiskLevel: LicenseRiskLevel.Medium,
        licenseDetails: """{"deps":[{"name":"Newtonsoft.Json","license":"MIT","compliant":true}]}""",
        conflicts: """{"conflicts":["GPL-3.0 incompatible with MIT in transitive chain"]}""",
        recommendations: """{"actions":["Replace package X with Y"]}""",
        tenantId: "tenant1",
        now: FixedNow);
}
