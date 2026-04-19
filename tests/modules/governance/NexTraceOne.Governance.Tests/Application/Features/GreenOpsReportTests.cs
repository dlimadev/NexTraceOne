using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetGreenOpsReport;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes para GetGreenOpsReport.Handler — valida comportamento com dados reais e modo de fallback.
/// </summary>
public sealed class GreenOpsReportTests
{
    private static IRuntimeIntelligenceModule CreateRuntimeMockWithData()
    {
        var mock = Substitute.For<IRuntimeIntelligenceModule>();
        mock.GetPlatformAverageMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new PlatformAverageMetrics(
                CurrentCpuPct: 40.0,
                CurrentMemoryPct: 60.0,
                ForecastedCpuPct: 50.0,
                ForecastedMemoryPct: 70.0,
                CpuTrend: "increasing",
                MemoryTrend: "stable",
                SampleCount: 100));
        return mock;
    }

    private static IRuntimeIntelligenceModule CreateRuntimeMockNoData()
    {
        var mock = Substitute.For<IRuntimeIntelligenceModule>();
        mock.GetPlatformAverageMetricsAsync(Arg.Any<CancellationToken>())
            .Returns((PlatformAverageMetrics?)null);
        return mock;
    }

    private static ICostIntelligenceModule CreateCostMockWithData()
    {
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CostRecordSummary[]
            {
                new("svc-api", "API Service", "team-a", "commerce", "Production", 8000m, "USD", "2026-04", "azure"),
                new("svc-worker", "Worker Service", "team-b", "operations", "Production", 4000m, "USD", "2026-04", "azure"),
                new("svc-db", "DB Service", "team-a", "data", "Production", 3000m, "USD", "2026-04", "azure"),
            });
        return mock;
    }

    private static IGreenOpsConfigurationRepository CreateEmptyConfigRepo()
    {
        var mock = Substitute.For<IGreenOpsConfigurationRepository>();
        mock.GetActiveAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.Governance.Domain.Entities.GreenOpsConfiguration?)null);
        return mock;
    }

    private static IConfiguration CreateFallbackConfig()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:GreenOps:IntensityFactorKgPerKwh"] = "0.233",
                ["Platform:GreenOps:EsgTargetKgCo2PerMonth"] = "100",
                ["Platform:GreenOps:DatacenterRegion"] = "eu-west-1"
            })
            .Build();

    [Fact]
    public async Task Handler_WithRealRuntimeData_ShouldReturnNonZeroEmissions()
    {
        var handler = new GetGreenOpsReport.Handler(
            CreateRuntimeMockWithData(),
            CreateCostMockWithData(),
            CreateEmptyConfigRepo(),
            CreateFallbackConfig(),
            NullLogger<GetGreenOpsReport.Handler>.Instance);

        var result = await handler.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalKgCo2.Should().BeGreaterThan(0, "real runtime data should produce non-zero emissions");
        result.Value.SimulatedNote.Should().BeEmpty("when real data is available, SimulatedNote must be empty");
    }

    [Fact]
    public async Task Handler_WithRealRuntimeData_ShouldPopulateTopServices()
    {
        var handler = new GetGreenOpsReport.Handler(
            CreateRuntimeMockWithData(),
            CreateCostMockWithData(),
            CreateEmptyConfigRepo(),
            CreateFallbackConfig(),
            NullLogger<GetGreenOpsReport.Handler>.Instance);

        var result = await handler.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopServices.Should().NotBeEmpty("cost records are available to attribute emissions");
        result.Value.TopServices.Should().OnlyContain(s => s.KgCo2 >= 0);
    }

    [Fact]
    public async Task Handler_WithNoRuntimeData_ShouldReturnSimulatedNote()
    {
        var handler = new GetGreenOpsReport.Handler(
            CreateRuntimeMockNoData(),
            CreateCostMockWithData(),
            CreateEmptyConfigRepo(),
            CreateFallbackConfig(),
            NullLogger<GetGreenOpsReport.Handler>.Instance);

        var result = await handler.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SimulatedNote.Should().NotBeEmpty("no runtime data means we're in simulated mode");
        result.Value.TotalKgCo2.Should().Be(0.0, "no data means zero emissions");
    }

    [Fact]
    public async Task Handler_WithIncreasingCpuTrend_ShouldReportIncreasingTrend()
    {
        var handler = new GetGreenOpsReport.Handler(
            CreateRuntimeMockWithData(),
            CreateCostMockWithData(),
            CreateEmptyConfigRepo(),
            CreateFallbackConfig(),
            NullLogger<GetGreenOpsReport.Handler>.Instance);

        var result = await handler.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);

        result.Value.Trend.Should().Be("increasing", "CPU trend is increasing");
    }

    [Fact]
    public async Task Handler_ShouldApplyIntensityFactorFromConfig()
    {
        var highIntensityConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:GreenOps:IntensityFactorKgPerKwh"] = "1.0",
                ["Platform:GreenOps:EsgTargetKgCo2PerMonth"] = "100",
                ["Platform:GreenOps:DatacenterRegion"] = "us-east-1"
            })
            .Build();

        var lowIntensityConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:GreenOps:IntensityFactorKgPerKwh"] = "0.1",
                ["Platform:GreenOps:EsgTargetKgCo2PerMonth"] = "100",
                ["Platform:GreenOps:DatacenterRegion"] = "eu-west-1"
            })
            .Build();

        var runtimeMock = CreateRuntimeMockWithData();
        var costMock = CreateCostMockWithData();

        var handlerHigh = new GetGreenOpsReport.Handler(runtimeMock, costMock, CreateEmptyConfigRepo(), highIntensityConfig, NullLogger<GetGreenOpsReport.Handler>.Instance);
        var handlerLow = new GetGreenOpsReport.Handler(runtimeMock, costMock, CreateEmptyConfigRepo(), lowIntensityConfig, NullLogger<GetGreenOpsReport.Handler>.Instance);

        var high = await handlerHigh.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);
        var low = await handlerLow.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);

        high.Value.TotalKgCo2.Should().BeGreaterThan(low.Value.TotalKgCo2,
            "higher intensity factor should yield more CO2 for the same workload");
    }

    [Fact]
    public async Task Handler_WhenRuntimeThrows_ShouldReturnSimulatedNote()
    {
        var faultyRuntime = Substitute.For<IRuntimeIntelligenceModule>();
        faultyRuntime.GetPlatformAverageMetricsAsync(Arg.Any<CancellationToken>())
            .Returns<PlatformAverageMetrics?>(_ => throw new InvalidOperationException("Simulated failure"));

        var handler = new GetGreenOpsReport.Handler(
            faultyRuntime,
            CreateCostMockWithData(),
            CreateEmptyConfigRepo(),
            CreateFallbackConfig(),
            NullLogger<GetGreenOpsReport.Handler>.Instance);

        var result = await handler.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("handler must not propagate internal failures");
        result.Value.SimulatedNote.Should().NotBeEmpty("when runtime throws, fallback simulated note is expected");
    }

    [Fact]
    public async Task Handler_ConfigSource_ShouldPreferPersistedOverIConfiguration()
    {
        var persistedConfig = NexTraceOne.Governance.Domain.Entities.GreenOpsConfiguration.Create(
            intensityFactorKgPerKwh: 0.5,
            esgTargetKgCo2PerMonth: 200.0,
            datacenterRegion: "us-east-1",
            now: DateTimeOffset.UtcNow);

        var repoWithData = Substitute.For<IGreenOpsConfigurationRepository>();
        repoWithData.GetActiveAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(persistedConfig);

        var fallback = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:GreenOps:IntensityFactorKgPerKwh"] = "0.1", // should be ignored
                ["Platform:GreenOps:DatacenterRegion"] = "eu-west-1",  // should be ignored
            })
            .Build();

        var handler = new GetGreenOpsReport.Handler(
            CreateRuntimeMockWithData(),
            CreateCostMockWithData(),
            repoWithData,
            fallback,
            NullLogger<GetGreenOpsReport.Handler>.Instance);

        var result = await handler.Handle(new GetGreenOpsReport.Query(), CancellationToken.None);

        result.Value.Config.IntensityFactorKgPerKwh.Should().Be(0.5, "persisted config overrides IConfiguration");
        result.Value.Config.DatacenterRegion.Should().Be("us-east-1", "persisted region overrides fallback");
    }
}
