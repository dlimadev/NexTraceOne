using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetSecurityIncidentCorrelationReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance.Application.Features;

/// <summary>
/// Testes unitários para Wave AX.3 — GetSecurityIncidentCorrelationReport.
/// Cobre correlação de incidentes de segurança com CVEs, sinais de correlação e SecurityIncidentCorrelationRisk.
/// </summary>
public sealed class WaveAxSecurityIncidentCorrelationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ax-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetSecurityIncidentCorrelationReport.Handler CreateHandler(
        IReadOnlyList<ISecurityIncidentCorrelationReader.SecurityIncidentEntry> incidents)
    {
        var reader = Substitute.For<ISecurityIncidentCorrelationReader>();
        reader.ListSecurityIncidentsByTenantAsync(
            TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(incidents);
        return new GetSecurityIncidentCorrelationReport.Handler(reader, CreateClock());
    }

    private static ISecurityIncidentCorrelationReader.SecurityIncidentEntry MakeIncident(
        bool criticalCvePresent = false,
        bool vulnComponentRecent = false,
        int activeCveCount = 0,
        IReadOnlyList<string>? components = null) =>
        new(Guid.NewGuid(), "svc-ax", "service-ax", FixedNow.AddDays(-5),
            activeCveCount, criticalCvePresent, vulnComponentRecent, components ?? []);

    [Fact]
    public async Task AX3_EmptyReport_WhenNoIncidents()
    {
        var handler = CreateHandler([]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByIncident.Should().BeEmpty();
        result.Value.TenantSecurityIncidentSummary.SecurityIncidentCount.Should().Be(0);
        result.Value.TenantSecurityIncidentSummary.TenantCVEIncidentCorrelationRate.Should().Be(0m);
        result.Value.RiskReductionOpportunity.Should().Be(0);
    }

    [Fact]
    public async Task AX3_Risk_None_WhenNoSignals()
    {
        var incident = MakeIncident(criticalCvePresent: false, vulnComponentRecent: false, activeCveCount: 2);
        var handler = CreateHandler([incident]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.ByIncident[0].Risk.Should().Be(GetSecurityIncidentCorrelationReport.SecurityIncidentCorrelationRisk.None);
        result.Value.ByIncident[0].CorrelationSignals.Should().BeEmpty();
    }

    [Fact]
    public async Task AX3_Risk_Possible_WhenOneSignal()
    {
        // Only CriticalCvePresentAtTime=true → 1 signal → Possible
        var incident = MakeIncident(criticalCvePresent: true, vulnComponentRecent: false, activeCveCount: 2);
        var handler = CreateHandler([incident]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.ByIncident[0].Risk.Should().Be(GetSecurityIncidentCorrelationReport.SecurityIncidentCorrelationRisk.Possible);
        result.Value.ByIncident[0].CorrelationSignals.Should().ContainSingle(s => s == "unpatched_critical_cve_present");
    }

    [Fact]
    public async Task AX3_Risk_Possible_WhenElevatedCveExposure()
    {
        // Only ActiveCveCountAtTime=5 → 1 signal → Possible
        var incident = MakeIncident(criticalCvePresent: false, vulnComponentRecent: false, activeCveCount: 5);
        var handler = CreateHandler([incident]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.ByIncident[0].Risk.Should().Be(GetSecurityIncidentCorrelationReport.SecurityIncidentCorrelationRisk.Possible);
        result.Value.ByIncident[0].CorrelationSignals.Should().ContainSingle(s => s == "elevated_cve_exposure");
    }

    [Fact]
    public async Task AX3_Risk_Likely_WhenTwoSignals_NoCriticalAndNoVulnComponent()
    {
        // elevated_cve_exposure (activeCveCount>=5) + unpatched_critical (criticalPresent=true) = 2 signals
        // Not Strong because VulnerableComponentIntroducedRecently=false
        var incident = MakeIncident(criticalCvePresent: true, vulnComponentRecent: false, activeCveCount: 5);
        var handler = CreateHandler([incident]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.ByIncident[0].Risk.Should().Be(GetSecurityIncidentCorrelationReport.SecurityIncidentCorrelationRisk.Likely);
        result.Value.ByIncident[0].CorrelationSignals.Should().HaveCount(2);
    }

    [Fact]
    public async Task AX3_Risk_Strong_WhenAllThreeSignals()
    {
        // All 3 signals: critical=true, vulnComponent=true, activeCveCount=5
        var incident = MakeIncident(criticalCvePresent: true, vulnComponentRecent: true, activeCveCount: 5);
        var handler = CreateHandler([incident]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.ByIncident[0].Risk.Should().Be(GetSecurityIncidentCorrelationReport.SecurityIncidentCorrelationRisk.Strong);
        result.Value.ByIncident[0].CorrelationSignals.Should().HaveCount(3);
    }

    [Fact]
    public async Task AX3_Risk_Strong_WhenTwoSignalsWithCriticalAndVulnComponent()
    {
        // 2 signals: critical=true, vulnComponent=true, activeCveCount=2 (< 5) → Strong
        var incident = MakeIncident(criticalCvePresent: true, vulnComponentRecent: true, activeCveCount: 2);
        var handler = CreateHandler([incident]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.ByIncident[0].Risk.Should().Be(GetSecurityIncidentCorrelationReport.SecurityIncidentCorrelationRisk.Strong);
        result.Value.ByIncident[0].CorrelationSignals.Should().HaveCount(2);
    }

    [Fact]
    public async Task AX3_TenantCVEIncidentCorrelationRate_CalculatedCorrectly()
    {
        // 4 incidents: 3 with signals, 1 without → 75%
        var withSignal1 = MakeIncident(criticalCvePresent: true);
        var withSignal2 = MakeIncident(activeCveCount: 5);
        var withSignal3 = MakeIncident(vulnComponentRecent: true);
        var withoutSignal = MakeIncident(criticalCvePresent: false, vulnComponentRecent: false, activeCveCount: 1);

        var handler = CreateHandler([withSignal1, withSignal2, withSignal3, withoutSignal]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantSecurityIncidentSummary.TenantCVEIncidentCorrelationRate.Should().Be(75m);
    }

    [Fact]
    public async Task AX3_TenantCVEIncidentCorrelationRate_ZeroWhenNoIncidents()
    {
        var handler = CreateHandler([]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantSecurityIncidentSummary.TenantCVEIncidentCorrelationRate.Should().Be(0m);
    }

    [Fact]
    public async Task AX3_RiskReductionOpportunity_CountsLikelyAndStrong()
    {
        // 2 Strong + 1 Likely + 1 Possible + 1 None → RiskReductionOpportunity = 3
        var strong1 = MakeIncident(criticalCvePresent: true, vulnComponentRecent: true, activeCveCount: 2);
        var strong2 = MakeIncident(criticalCvePresent: true, vulnComponentRecent: true, activeCveCount: 5);
        var likely = MakeIncident(criticalCvePresent: true, vulnComponentRecent: false, activeCveCount: 5);
        var possible = MakeIncident(criticalCvePresent: true, activeCveCount: 0);
        var none = MakeIncident(criticalCvePresent: false, vulnComponentRecent: false, activeCveCount: 0);

        var handler = CreateHandler([strong1, strong2, likely, possible, none]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.RiskReductionOpportunity.Should().Be(3);
    }

    [Fact]
    public async Task AX3_CVEsWithIncidentCorrelation_OnlyMultiIncidentComponents()
    {
        // comp-A in 2 incidents, comp-B only in 1 → CVEsWithIncidentCorrelation = ["comp-A"]
        var incident1 = MakeIncident(components: ["comp-A", "comp-B"]);
        var incident2 = MakeIncident(components: ["comp-A"]);

        var handler = CreateHandler([incident1, incident2]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.CVEsWithIncidentCorrelation.Should().ContainSingle(c => c == "comp-A");
        result.Value.CVEsWithIncidentCorrelation.Should().NotContain("comp-B");
    }

    [Fact]
    public async Task AX3_ComponentsIntroducedBeforeIncident_AllDistinct()
    {
        var incident1 = MakeIncident(components: ["comp-A", "comp-B"]);
        var incident2 = MakeIncident(components: ["comp-A", "comp-C"]);

        var handler = CreateHandler([incident1, incident2]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.ComponentsIntroducedBeforeIncident.Should().BeEquivalentTo(new[] { "comp-A", "comp-B", "comp-C" });
    }

    [Fact]
    public async Task AX3_WithActiveUnpatchedCVE_CountsCorrectly()
    {
        // 3 incidents: 2 with CriticalCvePresentAtTime=true → WithActiveUnpatchedCVE=2
        var with1 = MakeIncident(criticalCvePresent: true);
        var with2 = MakeIncident(criticalCvePresent: true);
        var without = MakeIncident(criticalCvePresent: false);

        var handler = CreateHandler([with1, with2, without]);

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantSecurityIncidentSummary.WithActiveUnpatchedCVE.Should().Be(2);
    }

    [Fact]
    public async Task AX3_NullImpl_ReturnsEmptyReport()
    {
        var reader = new NexTraceOne.ChangeGovernance.Application.Compliance.NullSecurityIncidentCorrelationReader();
        var handler = new GetSecurityIncidentCorrelationReport.Handler(reader, CreateClock());

        var result = await handler.Handle(new GetSecurityIncidentCorrelationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByIncident.Should().BeEmpty();
        result.Value.RiskReductionOpportunity.Should().Be(0);
    }

    [Fact]
    public async Task AX3_Validator_Rejects_CorrelationWindowAbove168()
    {
        var validator = new GetSecurityIncidentCorrelationReport.Validator();
        var query = new GetSecurityIncidentCorrelationReport.Query(TenantId, CorrelationWindowHours: 200);

        var validationResult = await validator.ValidateAsync(query);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(query.CorrelationWindowHours));
    }
}
