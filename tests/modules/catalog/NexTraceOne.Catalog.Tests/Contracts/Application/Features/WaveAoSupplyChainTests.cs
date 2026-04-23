using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDependencyProvenanceReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSbomCoverageReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSupplyChainRiskReport;
using NexTraceOne.Catalog.Application.Contracts.Features.IngestSbomRecord;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AO — Supply Chain &amp; Dependency Provenance.
/// Cobre AO.1 IngestSbomRecord, AO.1 GetSbomCoverageReport,
/// AO.2 GetDependencyProvenanceReport e AO.3 GetSupplyChainRiskReport.
/// </summary>
public sealed class WaveAoSupplyChainTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ao-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AO.1 — IngestSbomRecord
    // ════════════════════════════════════════════════════════════════════════

    private static IngestSbomRecord.Handler CreateIngestHandler(ISbomRepository? repo = null)
    {
        repo ??= Substitute.For<ISbomRepository>();
        return new IngestSbomRecord.Handler(repo, CreateClock());
    }

    private static IngestSbomRecord.Command ValidIngestCommand(
        IReadOnlyList<IngestSbomRecord.ComponentInput>? components = null) =>
        new(TenantId, "svc-1", "ServiceA", "1.0.0",
            components ?? [new("Newtonsoft.Json", "13.0.3", "nuget.org", "MIT", 0, "None")]);

    [Fact]
    public async Task IngestSbomRecord_ValidCommand_ReturnsGuidAndCallsRepository()
    {
        var repo = Substitute.For<ISbomRepository>();
        var handler = CreateIngestHandler(repo);

        var result = await handler.Handle(ValidIngestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await repo.Received(1).AddAsync(Arg.Any<SbomRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestSbomRecord_RecordedAt_MatchesClock()
    {
        SbomRecord? captured = null;
        var repo = Substitute.For<ISbomRepository>();
        repo.When(r => r.AddAsync(Arg.Any<SbomRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<SbomRecord>());
        var handler = CreateIngestHandler(repo);

        await handler.Handle(ValidIngestCommand(), CancellationToken.None);

        captured!.RecordedAt.Should().Be(FixedNow);
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("ServiceId")]
    [InlineData("Version")]
    public void IngestSbomRecord_EmptyRequiredField_ValidationFails(string fieldName)
    {
        var validator = new IngestSbomRecord.Validator();
        var command = fieldName switch
        {
            "TenantId" => ValidIngestCommand() with { TenantId = "" },
            "ServiceId" => ValidIngestCommand() with { ServiceId = "" },
            "Version"   => ValidIngestCommand() with { Version = "" },
            _           => ValidIngestCommand()
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IngestSbomRecord_NullComponents_ValidationFails()
    {
        var validator = new IngestSbomRecord.Validator();
        var command = new IngestSbomRecord.Command(TenantId, "svc-1", "Svc", "1.0", null!);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AO.1 — GetSbomCoverageReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetSbomCoverageReport.Handler CreateCoverageHandler(
        IReadOnlyList<ISbomCoverageReader.ServiceSbomEntry> entries)
    {
        var reader = Substitute.For<ISbomCoverageReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetSbomCoverageReport.Handler(reader, CreateClock());
    }

    private static ISbomCoverageReader.ServiceSbomEntry MakeSbomEntry(
        string serviceId, DateTimeOffset? lastRecordedAt,
        int criticalCves = 0, IReadOnlyList<string>? gpl = null) =>
        new(serviceId, $"Service-{serviceId}", "team-a", "Gold", false,
            10, 0, criticalCves, 0,
            new Dictionary<string, int> { ["MIT"] = 8, ["Apache"] = 2 },
            lastRecordedAt, gpl ?? []);

    private static GetSbomCoverageReport.Query DefaultCoverageQuery(int fresh = 30, int stale = 90) =>
        new(TenantId, fresh, stale);

    [Fact]
    public async Task GetSbomCoverageReport_EmptyReader_ReturnsTotalServicesZeroAndCoveredPctZero()
    {
        var handler = CreateCoverageHandler([]);
        var result = await handler.Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(0);
        result.Value.Summary.CoveredPct.Should().Be(0m);
        result.Value.ByService.Should().BeEmpty();
        result.Value.LicenseRiskFlags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSbomCoverageReport_SbomAge15d_FreshDays30_ReturnsCoveredTier()
    {
        var entry = MakeSbomEntry("s-1", FixedNow.AddDays(-15));
        var result = await CreateCoverageHandler([entry])
            .Handle(DefaultCoverageQuery(fresh: 30), CancellationToken.None);

        result.Value.ByService.Single().Tier.Should().Be(GetSbomCoverageReport.SbomCoverageTier.Covered);
    }

    [Fact]
    public async Task GetSbomCoverageReport_SbomAge45d_StaleDays90_ReturnsStale()
    {
        var entry = MakeSbomEntry("s-2", FixedNow.AddDays(-45));
        var result = await CreateCoverageHandler([entry])
            .Handle(DefaultCoverageQuery(fresh: 30, stale: 90), CancellationToken.None);

        result.Value.ByService.Single().Tier.Should().Be(GetSbomCoverageReport.SbomCoverageTier.Stale);
    }

    [Fact]
    public async Task GetSbomCoverageReport_SbomAge100d_StaleDays90_ReturnsOutdated()
    {
        var entry = MakeSbomEntry("s-3", FixedNow.AddDays(-100));
        var result = await CreateCoverageHandler([entry])
            .Handle(DefaultCoverageQuery(fresh: 30, stale: 90), CancellationToken.None);

        result.Value.ByService.Single().Tier.Should().Be(GetSbomCoverageReport.SbomCoverageTier.Outdated);
    }

    [Fact]
    public async Task GetSbomCoverageReport_NoSbom_ReturnsMissingTier()
    {
        var entry = MakeSbomEntry("s-4", null);
        var result = await CreateCoverageHandler([entry])
            .Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.Value.ByService.Single().Tier.Should().Be(GetSbomCoverageReport.SbomCoverageTier.Missing);
    }

    [Fact]
    public async Task GetSbomCoverageReport_OneCoveredOutOfTwo_CoveredPctFifty()
    {
        var entries = new[]
        {
            MakeSbomEntry("s-5", FixedNow.AddDays(-10)),
            MakeSbomEntry("s-6", FixedNow.AddDays(-60))
        };
        var result = await CreateCoverageHandler(entries)
            .Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.Value.Summary.CoveredPct.Should().Be(50m);
    }

    [Fact]
    public async Task GetSbomCoverageReport_TotalCriticalCves_IsSumAcrossAllServices()
    {
        var entries = new[]
        {
            MakeSbomEntry("s-7", FixedNow.AddDays(-5), criticalCves: 3),
            MakeSbomEntry("s-8", FixedNow.AddDays(-5), criticalCves: 7)
        };
        var result = await CreateCoverageHandler(entries)
            .Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.Value.Summary.TotalCriticalCves.Should().Be(10);
    }

    [Fact]
    public async Task GetSbomCoverageReport_TopVulnerableServices_CappedAtFiveAndSortedByCritical()
    {
        var entries = Enumerable.Range(1, 7)
            .Select(i => MakeSbomEntry($"sv-{i}", FixedNow.AddDays(-5), criticalCves: i))
            .ToList();
        var result = await CreateCoverageHandler(entries)
            .Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.Value.Summary.TopVulnerableServices.Should().HaveCount(5);
        result.Value.Summary.TopVulnerableServices.First().CriticalCveCount.Should().Be(7);
    }

    [Fact]
    public async Task GetSbomCoverageReport_LicenseRiskFlags_OnlyServicesWithGplAgpl()
    {
        var entries = new[]
        {
            MakeSbomEntry("s-gpl", FixedNow.AddDays(-5), gpl: ["spring-core"]),
            MakeSbomEntry("s-clean", FixedNow.AddDays(-5), gpl: [])
        };
        var result = await CreateCoverageHandler(entries)
            .Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.Value.LicenseRiskFlags.Should().HaveCount(1);
        result.Value.LicenseRiskFlags.Single().ServiceId.Should().Be("s-gpl");
    }

    [Fact]
    public void GetSbomCoverageReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetSbomCoverageReport.Validator();
        var result = validator.Validate(new GetSbomCoverageReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSbomCoverageReport_FreshDaysZero_ValidationFails()
    {
        var validator = new GetSbomCoverageReport.Validator();
        var result = validator.Validate(new GetSbomCoverageReport.Query(TenantId, FreshDays: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetSbomCoverageReport_MultipleTiers_CorrectDistribution()
    {
        var entries = new[]
        {
            MakeSbomEntry("s-a", FixedNow.AddDays(-10)),   // Covered
            MakeSbomEntry("s-b", FixedNow.AddDays(-50)),   // Stale
            MakeSbomEntry("s-c", FixedNow.AddDays(-100)),  // Outdated
            MakeSbomEntry("s-d", null)                     // Missing
        };
        var result = await CreateCoverageHandler(entries)
            .Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.Value.ByService.Count(r => r.Tier == GetSbomCoverageReport.SbomCoverageTier.Covered).Should().Be(1);
        result.Value.ByService.Count(r => r.Tier == GetSbomCoverageReport.SbomCoverageTier.Stale).Should().Be(1);
        result.Value.ByService.Count(r => r.Tier == GetSbomCoverageReport.SbomCoverageTier.Outdated).Should().Be(1);
        result.Value.ByService.Count(r => r.Tier == GetSbomCoverageReport.SbomCoverageTier.Missing).Should().Be(1);
    }

    [Fact]
    public async Task GetSbomCoverageReport_MissingServices_ExcludedFromTopVulnerable()
    {
        var entries = new[]
        {
            MakeSbomEntry("s-missing", null, criticalCves: 99),
            MakeSbomEntry("s-covered", FixedNow.AddDays(-5), criticalCves: 1)
        };
        var result = await CreateCoverageHandler(entries)
            .Handle(DefaultCoverageQuery(), CancellationToken.None);

        result.Value.Summary.TopVulnerableServices.Should().NotContain(r => r.ServiceId == "s-missing");
    }

    // ════════════════════════════════════════════════════════════════════════
    // AO.2 — GetDependencyProvenanceReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetDependencyProvenanceReport.Handler CreateProvenanceHandler(
        IReadOnlyList<IDependencyProvenanceReader.ComponentProvenanceEntry> entries)
    {
        var reader = Substitute.For<IDependencyProvenanceReader>();
        reader.ListComponentsByTenantAsync(
                Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetDependencyProvenanceReport.Handler(reader, CreateClock());
    }

    private static IDependencyProvenanceReader.ComponentProvenanceEntry MakeComponent(
        string name, string registry, bool approved, string license,
        string highestSeverity = "None", int cveCount = 0, int serviceCount = 1) =>
        new(name, ["1.0.0"], serviceCount, registry, approved, license, cveCount, highestSeverity);

    private static GetDependencyProvenanceReport.Query DefaultProvenanceQuery() =>
        new(TenantId);

    [Fact]
    public async Task GetDependencyProvenanceReport_EmptyReader_ReturnsZeroComponentsAndAllZeroSummary()
    {
        var result = await CreateProvenanceHandler([])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalComponents.Should().Be(0);
        result.Value.Summary.TrustedPct.Should().Be(0m);
        result.Value.Summary.UnapprovedRegistryComponents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_MitLicenseApprovedRegistry_NoVulns_ReturnsTrusted()
    {
        var entry = MakeComponent("Newtonsoft.Json", "nuget.org", true, "MIT");
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.Trusted);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_GplLicense_ReturnsHighRisk()
    {
        var entry = MakeComponent("some-gpl-lib", "nuget.org", true, "GPL-3.0");
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.HighRisk);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_AgplLicense_ReturnsHighRisk()
    {
        var entry = MakeComponent("agpl-lib", "nuget.org", true, "AGPL-3.0");
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.HighRisk);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_LgplLicense_ReturnsReview()
    {
        var entry = MakeComponent("lgpl-lib", "nuget.org", true, "LGPL-2.1");
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.Review);
        result.Value.ByComponent.Single().LicenseRisk.Should().Be(GetDependencyProvenanceReport.LicenseRisk.Attention);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_UnapprovedRegistryAndGpl_ReturnsBlocked()
    {
        var entry = MakeComponent("evil-lib", "unknown-registry.io", false, "GPL-3.0");
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.Blocked);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_CriticalCve_ReturnsBlocked()
    {
        var entry = MakeComponent("vuln-lib", "nuget.org", true, "MIT", "Critical", cveCount: 2);
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.Blocked);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_ApprovedRegistryHighCve_ReturnsReview()
    {
        var entry = MakeComponent("high-lib", "nuget.org", true, "MIT", "High", cveCount: 1);
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.Review);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_TrustedPct_CalculatedCorrectly()
    {
        var entries = new[]
        {
            MakeComponent("trusted-1", "nuget.org", true, "MIT"),
            MakeComponent("trusted-2", "nuget.org", true, "Apache-2.0"),
            MakeComponent("risky-1", "nuget.org", true, "GPL-3.0")
        };
        var result = await CreateProvenanceHandler(entries)
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.Summary.TrustedPct.Should().BeApproximately(66.67m, 0.01m);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_MostUsedComponents_CappedAt20AndSortedByServiceCount()
    {
        var entries = Enumerable.Range(1, 25)
            .Select(i => MakeComponent($"comp-{i}", "nuget.org", true, "MIT", serviceCount: i))
            .ToList();
        var result = await CreateProvenanceHandler(entries)
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.MostUsedComponents.Should().HaveCount(20);
        result.Value.MostUsedComponents.First().ServiceCount.Should().Be(25);
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_SinglePointOfFailure_MatchesThreshold()
    {
        var entries = new[]
        {
            MakeComponent("ubiquitous", "nuget.org", true, "MIT", serviceCount: 10),
            MakeComponent("rare-lib", "nuget.org", true, "MIT", serviceCount: 2)
        };
        var query = new GetDependencyProvenanceReport.Query(TenantId, SpofServiceThreshold: 5);
        var result = await CreateProvenanceHandler(entries).Handle(query, CancellationToken.None);

        result.Value.SinglePointOfFailureComponents.Should().HaveCount(1);
        result.Value.SinglePointOfFailureComponents.Single().ComponentName.Should().Be("ubiquitous");
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_UnapprovedRegistryComponents_OnlyNonApproved()
    {
        var entries = new[]
        {
            MakeComponent("approved-lib", "nuget.org", true, "MIT"),
            MakeComponent("unapproved-lib", "shady-registry.io", false, "MIT")
        };
        var result = await CreateProvenanceHandler(entries)
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.Summary.UnapprovedRegistryComponents.Should().HaveCount(1);
        result.Value.Summary.UnapprovedRegistryComponents.Single().ComponentName.Should().Be("unapproved-lib");
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_HighRiskLicenseComponents_OnlyGplAgpl()
    {
        var entries = new[]
        {
            MakeComponent("mit-lib", "nuget.org", true, "MIT"),
            MakeComponent("gpl-lib", "nuget.org", true, "GPL-3.0")
        };
        var result = await CreateProvenanceHandler(entries)
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.Summary.HighRiskLicenseComponents.Should().HaveCount(1);
        result.Value.Summary.HighRiskLicenseComponents.Single().ComponentName.Should().Be("gpl-lib");
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_CriticalVulnerabilityComponents_OnlyCriticalWithCves()
    {
        var entries = new[]
        {
            MakeComponent("safe-lib", "nuget.org", true, "MIT"),
            MakeComponent("critical-lib", "nuget.org", true, "MIT", "Critical", cveCount: 1)
        };
        var result = await CreateProvenanceHandler(entries)
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.Summary.CriticalVulnerabilityComponents.Should().HaveCount(1);
        result.Value.Summary.CriticalVulnerabilityComponents.Single().ComponentName.Should().Be("critical-lib");
    }

    [Fact]
    public void GetDependencyProvenanceReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetDependencyProvenanceReport.Validator();
        var result = validator.Validate(new GetDependencyProvenanceReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetDependencyProvenanceReport_UnknownLicense_ReturnsReview()
    {
        var entry = MakeComponent("mystery-lib", "nuget.org", true, "PROPRIETARY");
        var result = await CreateProvenanceHandler([entry])
            .Handle(DefaultProvenanceQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().LicenseRisk.Should().Be(GetDependencyProvenanceReport.LicenseRisk.Unknown);
        result.Value.ByComponent.Single().Tier.Should().Be(GetDependencyProvenanceReport.ProvenanceTier.Review);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AO.3 — GetSupplyChainRiskReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetSupplyChainRiskReport.Handler CreateRiskHandler(
        IReadOnlyList<ISupplyChainRiskReader.VulnerableComponentEntry> entries)
    {
        var reader = Substitute.For<ISupplyChainRiskReader>();
        reader.ListVulnerableComponentsByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetSupplyChainRiskReport.Handler(reader, CreateClock());
    }

    private static ISupplyChainRiskReader.VulnerableComponentEntry MakeVulnEntry(
        string name, string version, string severity,
        int cveCount = 1, string[]? direct = null, string[]? transitive = null,
        bool customerFacing = false, int totalServices = 10,
        DateTimeOffset? cvePublished = null, string? fixVersion = null) =>
        new(name, version, severity, cveCount, cvePublished, fixVersion,
            direct ?? ["svc-1"], transitive ?? [],
            customerFacing, totalServices);

    private static GetSupplyChainRiskReport.Query DefaultRiskQuery() => new(TenantId);

    [Fact]
    public async Task GetSupplyChainRiskReport_EmptyReader_ReturnsTenantScoreZeroAndSecureTier()
    {
        var result = await CreateRiskHandler([]).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalVulnerableComponents.Should().Be(0);
        result.Value.TenantSupplyChainRiskScore.Should().Be(0m);
        result.Value.TenantRiskTier.Should().Be(GetSupplyChainRiskReport.SupplyChainRiskTier.Secure);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_CriticalCveCustomerFacingAndHighExposure_ReturnsCriticalTier()
    {
        // Critical severity (100) * 0.5 + 100% exposure (100) * 0.3 + CustomerFacing (100) * 0.2
        // = 50 + 30 + 20 = 100 → Critical tier
        var entry = MakeVulnEntry("log4j", "2.14.0", "Critical",
            direct: ["s1", "s2", "s3", "s4", "s5", "s6", "s7", "s8", "s9", "s10"],
            customerFacing: true, totalServices: 10);
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetSupplyChainRiskReport.SupplyChainRiskTier.Critical);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_ComponentRiskScore_CalculatedCorrectly()
    {
        // Critical=100 * 0.5 = 50; exposure: 1/10=10% -> 10*0.3=3; cf: 0*0.2=0 → total=53
        var entry = MakeVulnEntry("test-pkg", "1.0.0", "Critical",
            direct: ["svc-1"], totalServices: 10, customerFacing: false);
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().ComponentRiskScore.Should().Be(53m);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_CriticalSeverityScore_IsOneHundred()
    {
        var entry = MakeVulnEntry("crit-lib", "1.0", "Critical",
            direct: [], transitive: [], totalServices: 0);
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        // severity 100 * 0.5 + exposure 0 * 0.3 + cf 0 * 0.2 = 50
        result.Value.ByComponent.Single().ComponentRiskScore.Should().Be(50m);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_HighSeverityScore_IsSeventyFive()
    {
        var entry = MakeVulnEntry("high-lib", "1.0", "High",
            direct: [], transitive: [], totalServices: 0);
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        // 75 * 0.5 = 37.5
        result.Value.ByComponent.Single().ComponentRiskScore.Should().Be(37.5m);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_TenantRiskScore_IsAverageOfComponentScores()
    {
        var entries = new[]
        {
            MakeVulnEntry("comp-a", "1.0", "Critical", direct: [], totalServices: 0), // score=50
            MakeVulnEntry("comp-b", "1.0", "High",     direct: [], totalServices: 0)  // score=37.5
        };
        var result = await CreateRiskHandler(entries).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.TenantSupplyChainRiskScore.Should().Be(43.75m);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_PrioritizedPatchList_SortedByRiskScoreDesc()
    {
        var entries = new[]
        {
            MakeVulnEntry("low-risk", "1.0", "Low",      direct: [], totalServices: 0),
            MakeVulnEntry("high-risk", "1.0", "Critical", direct: [], totalServices: 0)
        };
        var result = await CreateRiskHandler(entries).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.PrioritizedPatchList.First().ComponentName.Should().Be("high-risk");
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_UnpatchedWindowDays_CalculatedFromCvePublished()
    {
        var publishedAt = FixedNow.AddDays(-30);
        var entry = MakeVulnEntry("old-cve", "1.0", "High", cvePublished: publishedAt);
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().UnpatchedWindowDays.Should().Be(30);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_NoCvePublishedAt_UnpatchedWindowDaysIsNull()
    {
        var entry = MakeVulnEntry("no-date-cve", "1.0", "Medium");
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().UnpatchedWindowDays.Should().BeNull();
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_FixVersion_PropagatedToPatchPriorityItem()
    {
        var entry = MakeVulnEntry("patchable", "1.0", "High", fixVersion: "1.0.1");
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.PrioritizedPatchList.Single().FixVersion.Should().Be("1.0.1");
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_ScoreAtExactTierBoundary_Secure()
    {
        // Score exactly 0 → Secure
        var entry = MakeVulnEntry("zero-score", "1.0", "Low",
            direct: [], transitive: [], totalServices: 0, customerFacing: false);
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        // Low (25) * 0.5 = 12.5 → Secure (≤20)
        result.Value.ByComponent.Single().Tier.Should().Be(GetSupplyChainRiskReport.SupplyChainRiskTier.Secure);
    }

    [Fact]
    public async Task GetSupplyChainRiskReport_MonitoredTier_WhenScoreAbove20AndBelow50()
    {
        // Medium (50) * 0.5 = 25 → Monitored (>20 and ≤50)
        var entry = MakeVulnEntry("medium-lib", "1.0", "Medium",
            direct: [], transitive: [], totalServices: 0);
        var result = await CreateRiskHandler([entry]).Handle(DefaultRiskQuery(), CancellationToken.None);

        result.Value.ByComponent.Single().Tier.Should().Be(GetSupplyChainRiskReport.SupplyChainRiskTier.Monitored);
    }

    [Fact]
    public void GetSupplyChainRiskReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetSupplyChainRiskReport.Validator();
        var result = validator.Validate(new GetSupplyChainRiskReport.Query(""));
        result.IsValid.Should().BeFalse();
    }
}
