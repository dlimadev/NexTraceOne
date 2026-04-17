using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GenerateLicenseComplianceReport;
using NexTraceOne.Governance.Application.Features.GetControlsSummary;
using NexTraceOne.Governance.Application.Features.GetExecutiveOverview;
using NexTraceOne.Governance.Application.Features.GetExecutiveTrends;
using NexTraceOne.Governance.Application.Features.GetLicenseComplianceReport;
using NexTraceOne.Governance.Application.Features.GetMaturityScorecards;
using NexTraceOne.Governance.Application.Features.GetOnboardingContext;
using NexTraceOne.Governance.Application.Features.GetPlatformConfig;
using NexTraceOne.Governance.Application.Features.GetReportsSummary;
using NexTraceOne.Governance.Application.Features.GetRiskHeatmap;
using NexTraceOne.Governance.Application.Features.GetRiskSummary;
using NexTraceOne.Governance.Application.Features.GetScopedContext;
using NexTraceOne.Governance.Application.Features.ListLicenseComplianceReports;
using NexTraceOne.Governance.Application.Features.ListPackVersions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as 14 features de governança que não tinham cobertura.
/// Aumenta a cobertura do módulo Governance.
/// </summary>
public sealed class UncoveredGovernanceFeaturesTests
{
    // ── Shared mocks ──
    private readonly IGovernancePackRepository _packRepo = Substitute.For<IGovernancePackRepository>();
    private readonly IGovernanceWaiverRepository _waiverRepo = Substitute.For<IGovernanceWaiverRepository>();
    private readonly IGovernanceRolloutRecordRepository _rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
    private readonly IGovernancePackVersionRepository _versionRepo = Substitute.For<IGovernancePackVersionRepository>();
    private readonly ILicenseComplianceReportRepository _licenseRepo = Substitute.For<ILicenseComplianceReportRepository>();
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly IGovernanceDomainRepository _domainRepo = Substitute.For<IGovernanceDomainRepository>();
    private readonly IDelegatedAdministrationRepository _delegationRepo = Substitute.For<IDelegatedAdministrationRepository>();
    private readonly ITeamDomainLinkRepository _teamDomainLinkRepo = Substitute.For<ITeamDomainLinkRepository>();
    private readonly IGovernanceAnalyticsRepository _analyticsRepo = Substitute.For<IGovernanceAnalyticsRepository>();
    private readonly IIncidentModule _incidentModule = Substitute.For<IIncidentModule>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public UncoveredGovernanceFeaturesTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        SetupEmptyRepositories();
    }

    private void SetupEmptyRepositories()
    {
        _packRepo.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GovernancePack>());
        _waiverRepo.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GovernanceWaiver>());
        _rolloutRepo.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<GovernanceScopeType?>(), Arg.Any<string?>(), Arg.Any<RolloutStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GovernanceRolloutRecord>());
        _teamRepo.ListAsync(Arg.Any<TeamStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Team>());
        _incidentModule.GetTrendSummaryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new IncidentTrendSummary(0, 0, 0m, 0m, "Stable"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // 1. GenerateLicenseComplianceReport
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateLicenseComplianceReport_ValidData_ShouldReturnReport()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var handler = new GenerateLicenseComplianceReport.Handler(_licenseRepo, _unitOfWork, _clock);
        var command = new GenerateLicenseComplianceReport.Command(
            LicenseComplianceScope.Service, "svc-001", "Payment Service",
            100, 90, 5, 5, LicenseRiskLevel.Medium);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Scope.Should().Be(LicenseComplianceScope.Service);
        result.Value.ScopeKey.Should().Be("svc-001");
        result.Value.TotalDependencies.Should().Be(100);
        result.Value.CompliantCount.Should().Be(90);
        result.Value.OverallRiskLevel.Should().Be(LicenseRiskLevel.Medium);
        await _licenseRepo.Received(1).AddAsync(Arg.Any<LicenseComplianceReport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GenerateLicenseComplianceReport_Validator_InvalidScope_ShouldFail()
    {
        var validator = new GenerateLicenseComplianceReport.Validator();
        var command = new GenerateLicenseComplianceReport.Command(
            (LicenseComplianceScope)999, "", null, -1, 0, 0, 0, LicenseRiskLevel.Low);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Scope");
        result.Errors.Should().Contain(e => e.PropertyName == "ScopeKey");
        result.Errors.Should().Contain(e => e.PropertyName == "TotalDependencies");
    }

    [Fact]
    public void GenerateLicenseComplianceReport_Validator_ValidCommand_ShouldPass()
    {
        var validator = new GenerateLicenseComplianceReport.Validator();
        var command = new GenerateLicenseComplianceReport.Command(
            LicenseComplianceScope.Team, "team-01", "Backend Team",
            50, 45, 3, 2, LicenseRiskLevel.Low);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. GetLicenseComplianceReport
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetLicenseComplianceReport_Existing_ShouldReturnReport()
    {
        var report = LicenseComplianceReport.Generate(
            LicenseComplianceScope.Service, "svc-001", "Service A",
            50, 40, 5, 5, LicenseRiskLevel.Medium, null, null, null, null, DateTimeOffset.UtcNow);
        _licenseRepo.GetByIdAsync(Arg.Any<LicenseComplianceReportId>(), Arg.Any<CancellationToken>())
            .Returns(report);

        var handler = new GetLicenseComplianceReport.Handler(_licenseRepo);
        var result = await handler.Handle(new GetLicenseComplianceReport.Query(report.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScopeKey.Should().Be("svc-001");
    }

    [Fact]
    public async Task GetLicenseComplianceReport_NotFound_ShouldReturnError()
    {
        _licenseRepo.GetByIdAsync(Arg.Any<LicenseComplianceReportId>(), Arg.Any<CancellationToken>())
            .Returns((LicenseComplianceReport?)null);

        var handler = new GetLicenseComplianceReport.Handler(_licenseRepo);
        var result = await handler.Handle(new GetLicenseComplianceReport.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GetLicenseComplianceReport_Validator_EmptyId_ShouldFail()
    {
        var validator = new GetLicenseComplianceReport.Validator();
        var result = validator.Validate(new GetLicenseComplianceReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. ListLicenseComplianceReports
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListLicenseComplianceReports_EmptyResult_ShouldReturnEmptyList()
    {
        _licenseRepo.ListByScopeAsync(Arg.Any<LicenseComplianceScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LicenseComplianceReport>());

        var handler = new ListLicenseComplianceReports.Handler(_licenseRepo);
        var result = await handler.Handle(
            new ListLicenseComplianceReports.Query(LicenseComplianceScope.Service), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.FilteredScope.Should().Be(LicenseComplianceScope.Service);
    }

    [Fact]
    public async Task ListLicenseComplianceReports_WithReports_ShouldReturnAll()
    {
        var reports = new[]
        {
            LicenseComplianceReport.Generate(LicenseComplianceScope.Service, "svc-001", "Svc A", 10, 8, 1, 1, LicenseRiskLevel.Low, null, null, null, null, DateTimeOffset.UtcNow),
            LicenseComplianceReport.Generate(LicenseComplianceScope.Service, "svc-002", "Svc B", 20, 15, 3, 2, LicenseRiskLevel.Medium, null, null, null, null, DateTimeOffset.UtcNow),
        };
        _licenseRepo.ListByScopeAsync(Arg.Any<LicenseComplianceScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(reports);

        var handler = new ListLicenseComplianceReports.Handler(_licenseRepo);
        var result = await handler.Handle(
            new ListLicenseComplianceReports.Query(LicenseComplianceScope.Service), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public void ListLicenseComplianceReports_Validator_InvalidScope_ShouldFail()
    {
        var validator = new ListLicenseComplianceReports.Validator();
        var result = validator.Validate(new ListLicenseComplianceReports.Query((LicenseComplianceScope)999));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. GetControlsSummary
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetControlsSummary_NoPacks_ShouldReturnEmptyDimensions()
    {
        var handler = new GetControlsSummary.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetControlsSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dimensions.Should().BeEmpty();
        result.Value.OverallCoverage.Should().Be(0m);
        result.Value.CriticalGapCount.Should().Be(0);
    }

    [Fact]
    public async Task GetControlsSummary_WithPacks_ShouldComputeDimensions()
    {
        var pack = GovernancePack.Create("contract-std", "Contract Standards", null, GovernanceRuleCategory.Contracts);
        _packRepo.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { pack });

        var handler = new GetControlsSummary.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetControlsSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dimensions.Should().NotBeEmpty();
        result.Value.TotalDimensions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetControlsSummary_Validator_ValidQuery_ShouldPass()
    {
        var validator = new GetControlsSummary.Validator();
        var result = validator.Validate(new GetControlsSummary.Query("team-01", null, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetControlsSummary_Validator_TooLongTeamId_ShouldFail()
    {
        var validator = new GetControlsSummary.Validator();
        var result = validator.Validate(new GetControlsSummary.Query(new string('a', 201)));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. GetExecutiveOverview
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExecutiveOverview_EmptyData_ShouldReturnDefaultMetrics()
    {
        var handler = new GetExecutiveOverview.Handler(_packRepo, _waiverRepo, _rolloutRepo, _incidentModule);
        var result = await handler.Handle(new GetExecutiveOverview.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RiskSummary.Should().NotBeNull();
        result.Value.MaturitySummary.Should().NotBeNull();
        result.Value.OperationalTrend.Should().NotBeNull();
        result.Value.CrossModuleDataAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetExecutiveOverview_WithPacks_ShouldComputeRisk()
    {
        var pack = GovernancePack.Create("ops-rules", "Ops Rules", null, GovernanceRuleCategory.Operations);
        _packRepo.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { pack });

        var handler = new GetExecutiveOverview.Handler(_packRepo, _waiverRepo, _rolloutRepo, _incidentModule);
        var result = await handler.Handle(new GetExecutiveOverview.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ComplianceCoverageSummary.OverallScore.Should().Be(100m);
    }

    [Fact]
    public void GetExecutiveOverview_Validator_InvalidRange_ShouldFail()
    {
        var validator = new GetExecutiveOverview.Validator();
        var result = validator.Validate(new GetExecutiveOverview.Query(Range: "invalid"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetExecutiveOverview_Validator_ValidRange_ShouldPass()
    {
        var validator = new GetExecutiveOverview.Validator();
        var result = validator.Validate(new GetExecutiveOverview.Query(Range: "30d"));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. GetExecutiveTrends
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExecutiveTrends_ValidCategory_ShouldReturnSeries()
    {
        _analyticsRepo.GetWaiverCountsByMonthAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MonthlyCount>());
        _analyticsRepo.GetPublishedPackCountsByMonthAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MonthlyCount>());
        _analyticsRepo.GetRolloutCountsByMonthAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MonthlyCount>());

        var handler = new GetExecutiveTrends.Handler(_analyticsRepo);
        var result = await handler.Handle(new GetExecutiveTrends.Query("operations"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be("operations");
        result.Value.Series.Should().HaveCount(3);
        result.Value.IsSimulated.Should().BeFalse();
    }

    [Fact]
    public async Task GetExecutiveTrends_WithData_ShouldComputeTrends()
    {
        var months = new[]
        {
            new MonthlyCount("2025-01", 2),
            new MonthlyCount("2025-02", 3),
            new MonthlyCount("2025-03", 5),
            new MonthlyCount("2025-04", 8),
        };
        _analyticsRepo.GetWaiverCountsByMonthAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(months);
        _analyticsRepo.GetPublishedPackCountsByMonthAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(months);
        _analyticsRepo.GetRolloutCountsByMonthAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(months);

        var handler = new GetExecutiveTrends.Handler(_analyticsRepo);
        var result = await handler.Handle(new GetExecutiveTrends.Query("maturity"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Insights.Should().NotBeEmpty();
    }

    [Fact]
    public void GetExecutiveTrends_Validator_EmptyCategory_ShouldFail()
    {
        var validator = new GetExecutiveTrends.Validator();
        var result = validator.Validate(new GetExecutiveTrends.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 7. GetMaturityScorecards
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMaturityScorecards_NoTeams_ShouldReturnEmptyScorecards()
    {
        var handler = new GetMaturityScorecards.Handler(_teamRepo, _packRepo, _rolloutRepo, _waiverRepo);
        var result = await handler.Handle(new GetMaturityScorecards.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Scorecards.Should().BeEmpty();
        result.Value.Dimension.Should().Be("team");
    }

    [Fact]
    public async Task GetMaturityScorecards_WithTeams_ShouldComputeScorecards()
    {
        var team = Team.Create("backend", "Backend Team", null);
        _teamRepo.ListAsync(Arg.Any<TeamStatus?>(), Arg.Any<CancellationToken>()).Returns(new[] { team });

        var handler = new GetMaturityScorecards.Handler(_teamRepo, _packRepo, _rolloutRepo, _waiverRepo);
        var result = await handler.Handle(new GetMaturityScorecards.Query("team"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Scorecards.Should().HaveCount(1);
        result.Value.Scorecards[0].GroupName.Should().Be("Backend Team");
    }

    [Fact]
    public void GetMaturityScorecards_Validator_ValidDimension_ShouldPass()
    {
        var validator = new GetMaturityScorecards.Validator();
        var result = validator.Validate(new GetMaturityScorecards.Query("domain"));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 8. GetOnboardingContext
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("engineer")]
    [InlineData("tech_lead")]
    [InlineData("architect")]
    [InlineData("product")]
    [InlineData("executive")]
    [InlineData("platform_admin")]
    [InlineData("auditor")]
    public async Task GetOnboardingContext_ValidPersona_ShouldReturnQuickstartItems(string persona)
    {
        var handler = new GetOnboardingContext.Handler();
        var result = await handler.Handle(new GetOnboardingContext.Query(persona), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Persona.Should().Be(persona);
        result.Value.QuickstartItems.Should().NotBeEmpty();
        result.Value.RecommendedActions.Should().NotBeEmpty();
    }

    [Fact]
    public void GetOnboardingContext_Validator_InvalidPersona_ShouldFail()
    {
        var validator = new GetOnboardingContext.Validator();
        var result = validator.Validate(new GetOnboardingContext.Query("invalid_persona"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetOnboardingContext_Validator_EmptyPersona_ShouldFail()
    {
        var validator = new GetOnboardingContext.Validator();
        var result = validator.Validate(new GetOnboardingContext.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 9. GetPlatformConfig
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPlatformConfig_Default_ShouldReturnConfiguration()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var handler = new GetPlatformConfig.Handler(config);
        var result = await handler.Handle(new GetPlatformConfig.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FeatureFlags.Should().NotBeEmpty();
        result.Value.Subsystems.Should().NotBeEmpty();
        result.Value.Databases.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPlatformConfig_WithFeatureFlags_ShouldReadFromConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureFlags:ai-assistant"] = "true",
                ["FeatureFlags:finops-dashboard"] = "false",
            })
            .Build();

        var handler = new GetPlatformConfig.Handler(config);
        var result = await handler.Handle(new GetPlatformConfig.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FeatureFlags.Should().Contain(f => f.Name == "ai-assistant" && f.Enabled);
        result.Value.FeatureFlags.Should().Contain(f => f.Name == "finops-dashboard" && !f.Enabled);
    }

    [Fact]
    public async Task GetPlatformConfig_WithConnectionStrings_ShouldInferProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Primary"] = "Host=localhost;Port=5432;Database=nextraceone",
            })
            .Build();

        var handler = new GetPlatformConfig.Handler(config);
        var result = await handler.Handle(new GetPlatformConfig.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Databases.Should().Contain(d => d.Name == "Primary" && d.Provider == "PostgreSQL");
    }

    [Fact]
    public async Task GetPlatformConfig_WithEnvironment_ShouldReadEnvName()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            })
            .Build();

        var handler = new GetPlatformConfig.Handler(config);
        var result = await handler.Handle(new GetPlatformConfig.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EnvironmentName.Should().Be("Development");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 10. GetReportsSummary
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetReportsSummary_EmptyData_ShouldReturnZeros()
    {
        var handler = new GetReportsSummary.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetReportsSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPacks.Should().Be(0);
        result.Value.TotalWaivers.Should().Be(0);
        result.Value.TotalRollouts.Should().Be(0);
        result.Value.ComplianceScore.Should().Be(0m);
    }

    [Fact]
    public async Task GetReportsSummary_WithPacks_ShouldComputeCompliance()
    {
        var pack = GovernancePack.Create("std", "Standards", null, GovernanceRuleCategory.Contracts);
        _packRepo.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { pack });

        var handler = new GetReportsSummary.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetReportsSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPacks.Should().Be(1);
        result.Value.ComplianceScore.Should().Be(100m);
    }

    [Fact]
    public void GetReportsSummary_Validator_ValidQuery_ShouldPass()
    {
        var validator = new GetReportsSummary.Validator();
        var result = validator.Validate(new GetReportsSummary.Query(TeamId: "team-01"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetReportsSummary_Validator_TooLongValues_ShouldFail()
    {
        var validator = new GetReportsSummary.Validator();
        var result = validator.Validate(new GetReportsSummary.Query(TeamId: new string('x', 201)));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 11. GetRiskHeatmap
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRiskHeatmap_NoPacks_ShouldReturnEmptyCells()
    {
        var handler = new GetRiskHeatmap.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetRiskHeatmap.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Cells.Should().BeEmpty();
        result.Value.Dimension.Should().Be("category");
    }

    [Fact]
    public async Task GetRiskHeatmap_WithPacks_ShouldComputeRiskCells()
    {
        var pack = GovernancePack.Create("chg-rules", "Change Rules", null, GovernanceRuleCategory.Changes);
        _packRepo.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { pack });

        var handler = new GetRiskHeatmap.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetRiskHeatmap.Query("category"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Cells.Should().NotBeEmpty();
        result.Value.Cells[0].GroupName.Should().Be("Changes");
    }

    [Fact]
    public void GetRiskHeatmap_Validator_ValidDimension_ShouldPass()
    {
        var validator = new GetRiskHeatmap.Validator();
        var result = validator.Validate(new GetRiskHeatmap.Query("team"));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 12. GetRiskSummary
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRiskSummary_NoPacks_ShouldReturnLowRisk()
    {
        var handler = new GetRiskSummary.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetRiskSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallRiskLevel.Should().Be(RiskLevel.Low);
        result.Value.TotalPacksAssessed.Should().Be(0);
        result.Value.Indicators.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRiskSummary_WithPacks_ShouldAssessRisk()
    {
        var pack = GovernancePack.Create("ai-gov", "AI Governance", null, GovernanceRuleCategory.AIGovernance);
        _packRepo.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { pack });

        var handler = new GetRiskSummary.Handler(_packRepo, _waiverRepo, _rolloutRepo);
        var result = await handler.Handle(new GetRiskSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPacksAssessed.Should().Be(1);
        result.Value.Indicators.Should().HaveCount(1);
    }

    [Fact]
    public void GetRiskSummary_Validator_ValidQuery_ShouldPass()
    {
        var validator = new GetRiskSummary.Validator();
        var result = validator.Validate(new GetRiskSummary.Query("team-01", "domain-01"));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 13. GetScopedContext
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetScopedContext_Unauthenticated_ShouldReturnError()
    {
        _currentUser.IsAuthenticated.Returns(false);
        var handler = new GetScopedContext.Handler(
            _currentUser, _delegationRepo, _teamRepo, _teamDomainLinkRepo, _domainRepo);
        var result = await handler.Handle(new GetScopedContext.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetScopedContext_AuthenticatedNoDelegations_ShouldReturnEmptyScopes()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns("user-001");
        _delegationRepo.ListByGranteeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DelegatedAdministration>());

        var handler = new GetScopedContext.Handler(
            _currentUser, _delegationRepo, _teamRepo, _teamDomainLinkRepo, _domainRepo);
        var result = await handler.Handle(new GetScopedContext.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be("user-001");
        result.Value.AllowedTeams.Should().BeEmpty();
        result.Value.AllowedDomains.Should().BeEmpty();
        result.Value.IsCentralAdmin.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 14. ListPackVersions
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListPackVersions_ValidPackId_ShouldReturnVersions()
    {
        var handler = new ListPackVersions.Handler();
        var result = await handler.Handle(
            new ListPackVersions.Query("pack-001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Versions.Should().HaveCount(3);
        result.Value.Versions[0].Version.Should().Be("2.1.0");
        result.Value.Versions[2].Version.Should().Be("1.0.0");
    }

    [Fact]
    public void ListPackVersions_Validator_EmptyPackId_ShouldFail()
    {
        var validator = new ListPackVersions.Validator();
        var result = validator.Validate(new ListPackVersions.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListPackVersions_Validator_ValidPackId_ShouldPass()
    {
        var validator = new ListPackVersions.Validator();
        var result = validator.Validate(new ListPackVersions.Query("pack-001"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListPackVersions_Validator_TooLongPackId_ShouldFail()
    {
        var validator = new ListPackVersions.Validator();
        var result = validator.Validate(new ListPackVersions.Query(new string('a', 201)));
        result.IsValid.Should().BeFalse();
    }
}
