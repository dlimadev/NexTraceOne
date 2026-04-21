using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetResilienceScoreSummaryReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave P.2 — GetResilienceScoreSummaryReport.
/// Cobre: relatório vazio, score médio global, distribuição de tiers, top serviços resilientes,
/// top serviços vulneráveis, distribuição por tipo de experimento, recuperação e blast radius.
/// </summary>
public sealed class ResilienceScoreSummaryReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-resilience";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ResilienceReport MakeReport(
        string serviceName,
        int score,
        string experimentType = "latency-injection",
        int? recoveryTimeSeconds = null,
        decimal? blastRadiusDeviation = null)
        => ResilienceReport.Generate(
            chaosExperimentId: Guid.NewGuid(),
            serviceName: serviceName,
            environment: "production",
            experimentType: experimentType,
            resilienceScore: score,
            theoreticalBlastRadius: null,
            actualBlastRadius: null,
            blastRadiusDeviation: blastRadiusDeviation,
            telemetryObservations: null,
            latencyImpactMs: null,
            errorRateImpact: null,
            recoveryTimeSeconds: recoveryTimeSeconds,
            strengths: null,
            weaknesses: null,
            recommendations: null,
            tenantId: TenantId,
            generatedAt: FixedNow);

    private static GetResilienceScoreSummaryReport.Handler CreateHandler(
        IReadOnlyList<ResilienceReport> reports)
    {
        var repo = Substitute.For<IResilienceReportRepository>();
        repo.ListByServiceAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(reports);
        return new GetResilienceScoreSummaryReport.Handler(repo, CreateClock());
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_ResilienceReports()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReports.Should().Be(0);
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.OverallAvgResilienceScore.Should().Be(0m);
        result.Value.TopResilientServices.Should().BeEmpty();
        result.Value.TopVulnerableServices.Should().BeEmpty();
        result.Value.ByExperimentType.Should().BeEmpty();
    }

    // ── Overall average and tier ──────────────────────────────────────────

    [Fact]
    public async Task OverallAvgScore_Computed_Correctly()
    {
        var reports = new[]
        {
            MakeReport("svc-a", 90),
            MakeReport("svc-b", 70),
            MakeReport("svc-c", 50),
        };
        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.TotalReports.Should().Be(3);
        result.Value.OverallAvgResilienceScore.Should().Be(70.0m);
    }

    [Theory]
    [InlineData(90, GetResilienceScoreSummaryReport.ResilienceScoreTier.Excellent)]
    [InlineData(85, GetResilienceScoreSummaryReport.ResilienceScoreTier.Excellent)]
    [InlineData(65, GetResilienceScoreSummaryReport.ResilienceScoreTier.Good)]
    [InlineData(40, GetResilienceScoreSummaryReport.ResilienceScoreTier.Fair)]
    [InlineData(20, GetResilienceScoreSummaryReport.ResilienceScoreTier.Poor)]
    public async Task OverallScoreTier_Classified_Correctly(int score, GetResilienceScoreSummaryReport.ResilienceScoreTier expectedTier)
    {
        var handler = CreateHandler([MakeReport("svc-a", score)]);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.OverallScoreTier.Should().Be(expectedTier);
    }

    // ── Score distribution ────────────────────────────────────────────────

    [Fact]
    public async Task ScoreDistribution_Counts_Correctly()
    {
        var reports = new[]
        {
            MakeReport("svc-a", 90),  // Excellent
            MakeReport("svc-a", 70),  // Good
            MakeReport("svc-b", 50),  // Fair
            MakeReport("svc-b", 20),  // Poor
        };
        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.ScoreDistribution.ExcellentCount.Should().Be(1);
        result.Value.ScoreDistribution.GoodCount.Should().Be(1);
        result.Value.ScoreDistribution.FairCount.Should().Be(1);
        result.Value.ScoreDistribution.PoorCount.Should().Be(1);
    }

    // ── Top resilient vs top vulnerable ──────────────────────────────────

    [Fact]
    public async Task TopResilientServices_Ordered_ByAvgScore_Descending()
    {
        var reports = new[]
        {
            MakeReport("svc-low", 40),
            MakeReport("svc-high", 90),
            MakeReport("svc-mid", 65),
        };
        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.TopResilientServices[0].ServiceName.Should().Be("svc-high");
        result.Value.TopResilientServices[1].ServiceName.Should().Be("svc-mid");
        result.Value.TopResilientServices[2].ServiceName.Should().Be("svc-low");
    }

    [Fact]
    public async Task TopVulnerableServices_Ordered_ByAvgScore_Ascending()
    {
        var reports = new[]
        {
            MakeReport("svc-low", 40),
            MakeReport("svc-high", 90),
            MakeReport("svc-mid", 65),
        };
        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.TopVulnerableServices[0].ServiceName.Should().Be("svc-low");
        result.Value.TopVulnerableServices[1].ServiceName.Should().Be("svc-mid");
        result.Value.TopVulnerableServices[2].ServiceName.Should().Be("svc-high");
    }

    [Fact]
    public async Task TopServices_Limited_By_MaxTopServices()
    {
        var reports = Enumerable.Range(1, 10)
            .Select(i => MakeReport($"svc-{i}", i * 9))
            .ToList();

        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(MaxTopServices: 3), CancellationToken.None);

        result.Value.TopResilientServices.Should().HaveCount(3);
        result.Value.TopVulnerableServices.Should().HaveCount(3);
    }

    // ── Recovery time and blast radius ────────────────────────────────────

    [Fact]
    public async Task AvgRecoveryTimeSeconds_Computed_From_NonNull_Values()
    {
        var reports = new[]
        {
            MakeReport("svc-a", 80, recoveryTimeSeconds: 60),
            MakeReport("svc-a", 70, recoveryTimeSeconds: 120),
            MakeReport("svc-b", 60),  // no recovery time
        };
        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.AvgRecoveryTimeSeconds.Should().Be(90m);
    }

    [Fact]
    public async Task AvgRecoveryTimeSeconds_IsNull_When_No_NonNull_Values()
    {
        var handler = CreateHandler([MakeReport("svc-a", 80)]);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.AvgRecoveryTimeSeconds.Should().BeNull();
    }

    [Fact]
    public async Task AvgBlastRadiusDeviation_Computed_Correctly()
    {
        var reports = new[]
        {
            MakeReport("svc-a", 80, blastRadiusDeviation: 10m),
            MakeReport("svc-a", 70, blastRadiusDeviation: 30m),
        };
        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.AvgBlastRadiusDeviationPercent.Should().Be(20m);
    }

    // ── By experiment type ────────────────────────────────────────────────

    [Fact]
    public async Task ByExperimentType_Grouped_And_Counted_Correctly()
    {
        var reports = new[]
        {
            MakeReport("svc-a", 80, "latency-injection"),
            MakeReport("svc-b", 70, "latency-injection"),
            MakeReport("svc-c", 60, "pod-kill"),
        };
        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.ByExperimentType.Should().HaveCount(2);
        var latency = result.Value.ByExperimentType.First(e => e.ExperimentType == "latency-injection");
        latency.ReportCount.Should().Be(2);
        latency.AvgResilienceScore.Should().Be(75m);
    }

    [Fact]
    public async Task ByExperimentType_Limited_By_MaxTopExperimentTypes()
    {
        var reports = Enumerable.Range(1, 10)
            .Select(i => MakeReport("svc-a", 70, $"type-{i}"))
            .ToList();

        var handler = CreateHandler(reports);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(MaxTopExperimentTypes: 3), CancellationToken.None);

        result.Value.ByExperimentType.Should().HaveCount(3);
    }

    // ── GeneratedAt ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_GeneratedAt_Is_UtcNow()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(new GetResilienceScoreSummaryReport.Query(), CancellationToken.None);

        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 10)]
    [InlineData(101, 10)]
    [InlineData(10, 0)]
    [InlineData(10, 51)]
    public void Validator_Rejects_OutOfRange_Values(int maxTopServices, int maxTopExperimentTypes)
    {
        var validator = new GetResilienceScoreSummaryReport.Validator();
        var result = validator.Validate(new GetResilienceScoreSummaryReport.Query(
            MaxTopServices: maxTopServices,
            MaxTopExperimentTypes: maxTopExperimentTypes));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetResilienceScoreSummaryReport.Validator();
        var result = validator.Validate(new GetResilienceScoreSummaryReport.Query());
        result.IsValid.Should().BeTrue();
    }
}
