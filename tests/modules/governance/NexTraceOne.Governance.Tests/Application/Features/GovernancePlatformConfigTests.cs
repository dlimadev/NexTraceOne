using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.Features.GetAiGovernanceDashboard;
using NexTraceOne.Governance.Application.Features.GetCapacityForecast;
using NexTraceOne.Governance.Application.Features.GetGracefulShutdownConfig;
using NexTraceOne.Governance.Application.Features.GetObservabilityMode;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para os handlers de configuração de plataforma que não tinham cobertura:
/// GetCapacityForecast, GetAiGovernanceDashboard, GetObservabilityMode e GetGracefulShutdownConfig.
/// </summary>
public sealed class GovernancePlatformConfigTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static IConfigurationResolutionService CreateEmptyConfigService()
    {
        var svc = Substitute.For<IConfigurationResolutionService>();
        svc.ResolveEffectiveValueAsync(
                Arg.Any<string>(),
                Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.Configuration.Contracts.DTOs.EffectiveConfigurationDto?)null);
        return svc;
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetCapacityForecast
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCapacityForecast_NoMetrics_ShouldReturnFallbackForecasts()
    {
        var runtimeModule = Substitute.For<IRuntimeIntelligenceModule>();
        runtimeModule.GetPlatformAverageMetricsAsync(Arg.Any<CancellationToken>())
            .Returns((PlatformAverageMetrics?)null);

        var handler = new GetCapacityForecast.Handler(runtimeModule, CreateClock(), NullLogger<GetCapacityForecast.Handler>.Instance);
        var result = await handler.Handle(new GetCapacityForecast.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Forecasts.Should().HaveCount(4);
        result.Value.SimulatedNote.Should().NotBeNullOrWhiteSpace("fallback mode should have a simulation note");
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.NextReviewDate.Should().Be(FixedNow.AddDays(30));
    }

    [Fact]
    public async Task GetCapacityForecast_WithMetrics_ShouldReturnRealForecasts()
    {
        var runtimeModule = Substitute.For<IRuntimeIntelligenceModule>();
        runtimeModule.GetPlatformAverageMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new PlatformAverageMetrics(
                CurrentCpuPct: 45.0,
                CurrentMemoryPct: 62.0,
                ForecastedCpuPct: 55.0,
                ForecastedMemoryPct: 72.0,
                CpuTrend: "increasing",
                MemoryTrend: "stable",
                SampleCount: 200));

        var handler = new GetCapacityForecast.Handler(runtimeModule, CreateClock(), NullLogger<GetCapacityForecast.Handler>.Instance);
        var result = await handler.Handle(new GetCapacityForecast.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SimulatedNote.Should().BeNull("real metrics available — no simulation note expected");
        result.Value.GeneratedAt.Should().Be(FixedNow);
        var cpuForecast = result.Value.Forecasts.FirstOrDefault(f => f.Resource == "CPU");
        cpuForecast.Should().NotBeNull();
        cpuForecast!.ForecastedAt.Should().Be(FixedNow.AddDays(30));
    }

    [Fact]
    public async Task GetCapacityForecast_RiskClassification_CpuOver90_ShouldBeCritical()
    {
        var runtimeModule = Substitute.For<IRuntimeIntelligenceModule>();
        runtimeModule.GetPlatformAverageMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new PlatformAverageMetrics(
                CurrentCpuPct: 80.0,
                CurrentMemoryPct: 50.0,
                ForecastedCpuPct: 95.0,
                ForecastedMemoryPct: 55.0,
                CpuTrend: "increasing",
                MemoryTrend: "stable",
                SampleCount: 50));

        var handler = new GetCapacityForecast.Handler(runtimeModule, CreateClock(), NullLogger<GetCapacityForecast.Handler>.Instance);
        var result = await handler.Handle(new GetCapacityForecast.Query(), CancellationToken.None);

        var cpuForecast = result.Value.Forecasts.Single(f => f.Resource == "CPU");
        cpuForecast.Risk.Should().Be("Critical");
    }

    [Fact]
    public async Task GetCapacityForecast_WhenRuntimeModuleThrows_ShouldFallback()
    {
        var runtimeModule = Substitute.For<IRuntimeIntelligenceModule>();
        runtimeModule.GetPlatformAverageMetricsAsync(Arg.Any<CancellationToken>())
            .Returns<Task<PlatformAverageMetrics?>>(_ => throw new InvalidOperationException("runtime unavailable"));

        var handler = new GetCapacityForecast.Handler(runtimeModule, CreateClock(), NullLogger<GetCapacityForecast.Handler>.Instance);
        var result = await handler.Handle(new GetCapacityForecast.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("handler must degrade gracefully when runtime module is unavailable");
        result.Value.SimulatedNote.Should().NotBeNull();
        result.Value.Forecasts.Should().HaveCount(4);
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetAiGovernanceDashboard
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAiGovernanceDashboard_Handler_DefaultConfig_ShouldReturnDefaults()
    {
        var handler = new GetAiGovernanceDashboard.Handler(CreateEmptyConfigService(), CreateClock());
        var result = await handler.Handle(new GetAiGovernanceDashboard.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Config.GroundingCheckEnabled.Should().BeTrue("default when config not set");
        result.Value.Config.FeedbackEnabled.Should().BeTrue("default when config not set");
        result.Value.Config.AuditPromptsEnabled.Should().BeTrue("default when config not set");
        result.Value.ModelStats.Should().NotBeEmpty();
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetAiGovernanceDashboard_Handler_AllConfigDisabled_ShouldReflectFalse()
    {
        // When all AI governance config keys are set to false, all flags should be false
        var falseDto = new NexTraceOne.Configuration.Contracts.DTOs.EffectiveConfigurationDto(
            Key: "any",
            EffectiveValue: "false",
            ResolvedScope: "System",
            ResolvedScopeReferenceId: null,
            IsInherited: false,
            IsDefault: false,
            DefinitionKey: "any",
            ValueType: "bool",
            IsSensitive: false,
            Version: 1);

        var configService = Substitute.For<IConfigurationResolutionService>();
        configService.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(falseDto);

        var handler = new GetAiGovernanceDashboard.Handler(configService, CreateClock());
        var result = await handler.Handle(new GetAiGovernanceDashboard.Query(), CancellationToken.None);

        result.Value.Config.GroundingCheckEnabled.Should().BeFalse("all config keys set to false");
        result.Value.Config.FeedbackEnabled.Should().BeFalse("all config keys set to false");
        result.Value.Config.AuditPromptsEnabled.Should().BeFalse("all config keys set to false");
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetAiGovernanceDashboard_UpdateHandler_ShouldReturnConfiguredDashboard()
    {
        var handler = new GetAiGovernanceDashboard.UpdateHandler(CreateClock());
        var command = new GetAiGovernanceDashboard.UpdateAiGovernanceConfig(
            GroundingCheckEnabled: false,
            HallucinationFlagThreshold: 0.90,
            FeedbackEnabled: false,
            AuditPromptsEnabled: true,
            BlockExternalModelsOnSensitiveData: true,
            MaxTokenBudgetPerUser: 25000);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Config.GroundingCheckEnabled.Should().BeFalse();
        result.Value.Config.MaxTokenBudgetPerUser.Should().Be(25000);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetObservabilityMode
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetObservabilityMode_Handler_NoConfig_ShouldDefaultToLite()
    {
        var config = new ConfigurationBuilder().Build();
        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Lite");
        result.Value.ElasticsearchConnected.Should().BeFalse();
        result.Value.UpdatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetObservabilityMode_Handler_FullMode_ShouldReturnHigherRamUsage()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Platform:Observability:Mode"] = "Full" })
            .Build();
        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.Value.CurrentMode.Should().Be("Full");
        result.Value.AdditionalRamUsageGb.Should().Be(4.0);
        result.Value.TradeOffs.Should().Contain("Elasticsearch required");
    }

    [Fact]
    public async Task GetObservabilityMode_Handler_ElasticsearchConfigured_ShouldShowConnected()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Elasticsearch:Url"] = "http://localhost:9200" })
            .Build();
        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.Value.ElasticsearchConnected.Should().BeTrue();
        result.Value.Version.Should().Be("8.x");
    }

    [Fact]
    public async Task GetObservabilityMode_UpdateHandler_ShouldReturnUpdatedMode()
    {
        var handler = new GetObservabilityMode.UpdateHandler(CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.UpdateObservabilityMode("Full"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Full");
        result.Value.AdditionalRamUsageGb.Should().Be(4.0);
        result.Value.UpdatedAt.Should().Be(FixedNow);
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetGracefulShutdownConfig
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetGracefulShutdownConfig_Handler_NoConfig_ShouldReturnDefaults()
    {
        var config = new ConfigurationBuilder().Build();
        var handler = new GetGracefulShutdownConfig.Handler(config, CreateClock());
        var result = await handler.Handle(new GetGracefulShutdownConfig.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestDrainTimeoutSeconds.Should().Be(30);
        result.Value.OutboxDrainTimeoutSeconds.Should().Be(15);
        result.Value.HealthCheckReturns503OnShutdown.Should().BeTrue("default is true when not set");
        result.Value.AuditShutdownEvents.Should().BeTrue("default is true when not set");
        result.Value.UpdatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetGracefulShutdownConfig_Handler_CustomTimeouts_ShouldReturnConfiguredValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:GracefulShutdown:RequestDrainTimeoutSeconds"] = "60",
                ["Platform:GracefulShutdown:OutboxDrainTimeoutSeconds"] = "45",
                ["Platform:GracefulShutdown:HealthCheckReturns503OnShutdown"] = "false",
                ["Platform:GracefulShutdown:AuditShutdownEvents"] = "false"
            })
            .Build();

        var handler = new GetGracefulShutdownConfig.Handler(config, CreateClock());
        var result = await handler.Handle(new GetGracefulShutdownConfig.Query(), CancellationToken.None);

        result.Value.RequestDrainTimeoutSeconds.Should().Be(60);
        result.Value.OutboxDrainTimeoutSeconds.Should().Be(45);
        result.Value.HealthCheckReturns503OnShutdown.Should().BeFalse();
        result.Value.AuditShutdownEvents.Should().BeFalse();
    }

    [Fact]
    public async Task GetGracefulShutdownConfig_UpdateHandler_ShouldReturnUpdatedConfig()
    {
        var handler = new GetGracefulShutdownConfig.UpdateHandler(CreateClock());
        var command = new GetGracefulShutdownConfig.UpdateGracefulShutdownConfig(
            RequestDrainTimeoutSeconds: 120,
            OutboxDrainTimeoutSeconds: 30,
            HealthCheckReturns503OnShutdown: true,
            AuditShutdownEvents: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestDrainTimeoutSeconds.Should().Be(120);
        result.Value.OutboxDrainTimeoutSeconds.Should().Be(30);
        result.Value.AuditShutdownEvents.Should().BeFalse();
        result.Value.UpdatedAt.Should().Be(FixedNow);
    }
}
