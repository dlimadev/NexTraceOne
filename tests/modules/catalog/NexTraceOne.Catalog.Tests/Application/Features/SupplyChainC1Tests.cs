using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using IngestAdvisoryReportFeature = NexTraceOne.Catalog.Application.DependencyGovernance.Features.IngestAdvisoryReport.IngestAdvisoryReport;
using GetGateSummaryFeature = NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetServiceVulnerabilityGateSummary.GetServiceVulnerabilityGateSummary;
using EvaluateGateFeature = NexTraceOne.Catalog.Application.DependencyGovernance.Features.EvaluateVulnerabilityPromotionGate.EvaluateVulnerabilityPromotionGate;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave C.1 — Supply-chain Security.
/// Cobre IngestAdvisoryReport, GetServiceVulnerabilityGateSummary, EvaluateVulnerabilityPromotionGate
/// e VulnerabilityAdvisoryRecord domain entity.
/// </summary>
public sealed class SupplyChainC1Tests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock() =>
        Substitute.For<IDateTimeProvider>() is { } c
            ? (c.UtcNow.Returns(FixedNow), c).Item2
            : null!;

    private static IUnitOfWork CreateUnitOfWork() => Substitute.For<IUnitOfWork>();

    private static IngestAdvisoryReportFeature.AdvisoryInput MakeInput(
        string id = "CVE-2024-00001",
        VulnerabilitySeverity severity = VulnerabilitySeverity.High,
        decimal cvss = 7.5m,
        string? fixedIn = null) =>
        new(id, severity, cvss, $"Title for {id}", "NVD", FixedNow.AddDays(-10),
            FixedInVersion: fixedIn);

    // ── VulnerabilityAdvisoryRecord domain tests ──────────────────────────

    [Fact]
    public void Create_WithValidInputs_SetsPropertiesCorrectly()
    {
        var record = VulnerabilityAdvisoryRecord.Create(
            ServiceId, "cve-2024-00001", VulnerabilitySeverity.Critical,
            9.0m, "Critical Vuln", "GHSA", FixedNow.AddDays(-5), FixedNow);

        record.ServiceId.Should().Be(ServiceId);
        record.AdvisoryId.Should().Be("CVE-2024-00001"); // normalized to upper
        record.Severity.Should().Be(VulnerabilitySeverity.Critical);
        record.CvssScore.Should().Be(9.0m);
        record.IsActive.Should().BeTrue();
        record.Source.Should().Be("GHSA");
    }

    [Fact]
    public void Create_WithInvalidCvssScore_ThrowsException()
    {
        var act = () => VulnerabilityAdvisoryRecord.Create(
            ServiceId, "CVE-2024-00001", VulnerabilitySeverity.High,
            11m, "Title", "NVD", FixedNow, FixedNow); // cvss > 10

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_WithEmptyServiceId_ThrowsException()
    {
        var act = () => VulnerabilityAdvisoryRecord.Create(
            Guid.Empty, "CVE-2024-00001", VulnerabilitySeverity.High,
            7.5m, "Title", "NVD", FixedNow, FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var record = VulnerabilityAdvisoryRecord.Create(
            ServiceId, "CVE-2024-00002", VulnerabilitySeverity.Low,
            2.0m, "Low Vuln", "NVD", FixedNow, FixedNow);

        record.Deactivate();

        record.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateFixedInVersion_SetsVersion()
    {
        var record = VulnerabilityAdvisoryRecord.Create(
            ServiceId, "CVE-2024-00003", VulnerabilitySeverity.Medium,
            5.0m, "Medium Vuln", "NVD", FixedNow, FixedNow);

        record.UpdateFixedInVersion("1.2.3");

        record.FixedInVersion.Should().Be("1.2.3");
    }

    // ── IngestAdvisoryReport handler tests ───────────────────────────────

    [Fact]
    public async Task IngestAdvisoryReport_CreateNew_WhenRecordDoesNotExist()
    {
        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.FindByServiceAndAdvisoryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((VulnerabilityAdvisoryRecord?)null);

        var uow = CreateUnitOfWork();
        var clock = CreateClock();

        var handler = new IngestAdvisoryReportFeature.Handler(repo, uow, clock);
        var command = new IngestAdvisoryReportFeature.Command(ServiceId, [MakeInput()]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(1);
        result.Value.Updated.Should().Be(0);
        result.Value.ServiceId.Should().Be(ServiceId);
        await repo.Received(1).AddAsync(Arg.Any<VulnerabilityAdvisoryRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAdvisoryReport_IsIdempotent_WhenRecordAlreadyExistsWithoutFixedIn()
    {
        var existing = VulnerabilityAdvisoryRecord.Create(
            ServiceId, "CVE-2024-00001", VulnerabilitySeverity.High, 7.5m, "Title", "NVD", FixedNow, FixedNow);

        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.FindByServiceAndAdvisoryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new IngestAdvisoryReportFeature.Handler(repo, CreateUnitOfWork(), CreateClock());
        // Input has no fixedIn, existing has no fixedIn — no update
        var command = new IngestAdvisoryReportFeature.Command(ServiceId, [MakeInput()]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(0);
        result.Value.Updated.Should().Be(0);
        await repo.DidNotReceive().AddAsync(Arg.Any<VulnerabilityAdvisoryRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAdvisoryReport_UpdatesFixedInVersion_WhenInputProvidesItAndExistingDoesNot()
    {
        var existing = VulnerabilityAdvisoryRecord.Create(
            ServiceId, "CVE-2024-00001", VulnerabilitySeverity.High, 7.5m, "Title", "NVD", FixedNow, FixedNow);

        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.FindByServiceAndAdvisoryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new IngestAdvisoryReportFeature.Handler(repo, CreateUnitOfWork(), CreateClock());
        var command = new IngestAdvisoryReportFeature.Command(ServiceId, [MakeInput(fixedIn: "2.0.0")]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Updated.Should().Be(1);
        await repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        existing.FixedInVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task IngestAdvisoryReport_HandlesBatch_CreatesMultipleRecords()
    {
        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.FindByServiceAndAdvisoryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((VulnerabilityAdvisoryRecord?)null);

        var handler = new IngestAdvisoryReportFeature.Handler(repo, CreateUnitOfWork(), CreateClock());
        var command = new IngestAdvisoryReportFeature.Command(ServiceId, [
            MakeInput("CVE-2024-00001"),
            MakeInput("CVE-2024-00002"),
            MakeInput("CVE-2024-00003"),
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(3);
        await repo.Received(3).AddAsync(Arg.Any<VulnerabilityAdvisoryRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void IngestAdvisoryReport_Validator_RejectsEmptyServiceId()
    {
        var validator = new IngestAdvisoryReportFeature.Validator();
        var result = validator.Validate(new IngestAdvisoryReportFeature.Command(
            Guid.Empty, [MakeInput()]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceId");
    }

    [Fact]
    public void IngestAdvisoryReport_Validator_RejectsEmptyAdvisories()
    {
        var validator = new IngestAdvisoryReportFeature.Validator();
        var result = validator.Validate(new IngestAdvisoryReportFeature.Command(ServiceId, []));

        result.IsValid.Should().BeFalse();
    }

    // ── GetServiceVulnerabilityGateSummary tests ──────────────────────────

    [Fact]
    public async Task GetGateSummary_CountsCorrectlyBySeverity()
    {
        var records = new List<VulnerabilityAdvisoryRecord>
        {
            VulnerabilityAdvisoryRecord.Create(ServiceId, "CVE-001", VulnerabilitySeverity.Critical, 9.5m, "C1", "NVD", FixedNow, FixedNow),
            VulnerabilityAdvisoryRecord.Create(ServiceId, "CVE-002", VulnerabilitySeverity.Critical, 9.0m, "C2", "NVD", FixedNow, FixedNow),
            VulnerabilityAdvisoryRecord.Create(ServiceId, "CVE-003", VulnerabilitySeverity.High, 7.5m, "H1", "NVD", FixedNow, FixedNow),
            VulnerabilityAdvisoryRecord.Create(ServiceId, "CVE-004", VulnerabilitySeverity.Medium, 5.0m, "M1", "NVD", FixedNow, FixedNow),
            VulnerabilityAdvisoryRecord.Create(ServiceId, "CVE-005", VulnerabilitySeverity.Low, 2.0m, "L1", "NVD", FixedNow, FixedNow),
        };

        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.ListByServiceAsync(ServiceId, true, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<VulnerabilityAdvisoryRecord>)records);

        var handler = new GetGateSummaryFeature.Handler(repo);
        var result = await handler.Handle(new GetGateSummaryFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CriticalCount.Should().Be(2);
        result.Value.HighCount.Should().Be(1);
        result.Value.MediumCount.Should().Be(1);
        result.Value.LowCount.Should().Be(1);
        result.Value.TotalActiveAdvisories.Should().Be(5);
        result.Value.MostSevereCvssScore.Should().Be(9.5m);
    }

    [Fact]
    public async Task GetGateSummary_ReturnsZeros_WhenNoAdvisories()
    {
        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.ListByServiceAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<VulnerabilityAdvisoryRecord>)[]);

        var handler = new GetGateSummaryFeature.Handler(repo);
        var result = await handler.Handle(new GetGateSummaryFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalActiveAdvisories.Should().Be(0);
        result.Value.MostSevereCvssScore.Should().Be(0m);
    }

    // ── EvaluateVulnerabilityPromotionGate tests ──────────────────────────

    private static IConfigurationResolutionService CreateConfigService(string maxCritical = "0", string maxHigh = "5")
    {
        var cfg = Substitute.For<IConfigurationResolutionService>();
        cfg.ResolveEffectiveValueAsync(
                "security.vulnerability.gate.max_critical",
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto("security.vulnerability.gate.max_critical", maxCritical, "System", null, false, true, "label", "Integer", false, 1));
        cfg.ResolveEffectiveValueAsync(
                "security.vulnerability.gate.max_high",
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto("security.vulnerability.gate.max_high", maxHigh, "System", null, false, true, "label", "Integer", false, 1));
        return cfg;
    }

    [Fact]
    public async Task EvaluateGate_ReturnsPassed_WhenNoCriticalAndFewHigh()
    {
        var records = new List<VulnerabilityAdvisoryRecord>
        {
            VulnerabilityAdvisoryRecord.Create(ServiceId, "CVE-001", VulnerabilitySeverity.High, 7.5m, "H1", "NVD", FixedNow, FixedNow),
        };

        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.ListByServiceAsync(ServiceId, true, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<VulnerabilityAdvisoryRecord>)records);

        var handler = new EvaluateGateFeature.Handler(repo, CreateConfigService(maxCritical: "0", maxHigh: "5"));
        var result = await handler.Handle(new EvaluateGateFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GateResult.Should().Be(EvaluateGateFeature.VulnerabilityGateResult.Passed);
        result.Value.ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateGate_ReturnsBlockedByCritical_WhenCriticalExceedsMax()
    {
        var records = new List<VulnerabilityAdvisoryRecord>
        {
            VulnerabilityAdvisoryRecord.Create(ServiceId, "CVE-001", VulnerabilitySeverity.Critical, 9.5m, "C1", "NVD", FixedNow, FixedNow),
        };

        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.ListByServiceAsync(ServiceId, true, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<VulnerabilityAdvisoryRecord>)records);

        // maxCritical = 0, so 1 critical blocks
        var handler = new EvaluateGateFeature.Handler(repo, CreateConfigService(maxCritical: "0", maxHigh: "5"));
        var result = await handler.Handle(new EvaluateGateFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GateResult.Should().Be(EvaluateGateFeature.VulnerabilityGateResult.BlockedByCritical);
        result.Value.ShouldBlock.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateGate_ReturnsBlockedByHigh_WhenHighExceedsMax()
    {
        var records = Enumerable.Range(1, 6)
            .Select(i => VulnerabilityAdvisoryRecord.Create(ServiceId, $"CVE-HIGH-{i:000}", VulnerabilitySeverity.High, 7.5m, $"H{i}", "NVD", FixedNow, FixedNow))
            .ToList();

        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.ListByServiceAsync(ServiceId, true, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<VulnerabilityAdvisoryRecord>)records);

        // maxHigh = 5, so 6 high blocks
        var handler = new EvaluateGateFeature.Handler(repo, CreateConfigService(maxCritical: "0", maxHigh: "5"));
        var result = await handler.Handle(new EvaluateGateFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GateResult.Should().Be(EvaluateGateFeature.VulnerabilityGateResult.BlockedByHigh);
        result.Value.ShouldBlock.Should().BeTrue();
        result.Value.HighCount.Should().Be(6);
        result.Value.MaxHighAllowed.Should().Be(5);
    }

    [Fact]
    public async Task EvaluateGate_ReturnsPassed_WhenNoAdvisories()
    {
        var repo = Substitute.For<IVulnerabilityAdvisoryRepository>();
        repo.ListByServiceAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<VulnerabilityAdvisoryRecord>)[]);

        var handler = new EvaluateGateFeature.Handler(repo, CreateConfigService());
        var result = await handler.Handle(new EvaluateGateFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GateResult.Should().Be(EvaluateGateFeature.VulnerabilityGateResult.Passed);
        result.Value.ShouldBlock.Should().BeFalse();
    }
}
