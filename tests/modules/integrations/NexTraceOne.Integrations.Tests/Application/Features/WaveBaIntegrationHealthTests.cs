using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.GetIntegrationHealthReport;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave BA.3 — GetIntegrationHealthReport e NullIntegrationSyncReader.
/// </summary>
public sealed class WaveBaIntegrationHealthTests
{
    private readonly IIntegrationSyncReader _reader = Substitute.For<IIntegrationSyncReader>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private static readonly DateTimeOffset Now = new(2025, 10, 1, 10, 0, 0, TimeSpan.Zero);

    public WaveBaIntegrationHealthTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NullIntegrationSyncReader
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NullIntegrationSyncReader_ReturnsEmptyCollections()
    {
        var reader = new NullIntegrationSyncReader();

        var entries = await reader.ListByTenantAsync("t1", Now.AddHours(-72), Now, CancellationToken.None);
        var history = await reader.GetHealthHistoryAsync("t1", Now.AddDays(-7), Now, CancellationToken.None);

        entries.Should().BeEmpty();
        history.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetIntegrationHealthReport handler
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetIntegrationHealthReport_NoIntegrations_ReturnsEmpty()
    {
        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Integrations.Should().BeEmpty();
        result.Value.Summary.TenantIntegrationHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task GetIntegrationHealthReport_FreshHealthyIntegration_HealthyTier()
    {
        var entry = new IIntegrationSyncReader.IntegrationSyncEntry(
            "int-1", "GitLab", "GitLab",
            LastSyncAt: Now.AddMinutes(-30),
            TotalSyncsInWindow: 100,
            SuccessfulSyncsInWindow: 98,
            LastErrorMessage: null,
            IsCritical: true,
            AffectedFeatures: ["ChangeIntelligence", "ServiceCatalog"]);

        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integration = result.Value!.Integrations.Single();
        integration.HealthTier.Should().Be(GetIntegrationHealthReport.IntegrationHealthTier.Healthy);
        integration.FreshnessStatus.Should().Be(GetIntegrationHealthReport.DataFreshnessStatus.Fresh);
        result.Value.Summary.HealthyIntegrations.Should().Be(1);
        result.Value.Summary.TenantIntegrationHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task GetIntegrationHealthReport_StaleIntegration_StaleFreshnessStatus()
    {
        var entry = new IIntegrationSyncReader.IntegrationSyncEntry(
            "int-2", "Jenkins", "Jenkins",
            LastSyncAt: Now.AddHours(-60), // > 2×24h = Stale
            TotalSyncsInWindow: 10,
            SuccessfulSyncsInWindow: 9,
            LastErrorMessage: null,
            IsCritical: false,
            AffectedFeatures: []);

        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1", FreshnessHours: 24), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integration = result.Value!.Integrations.Single();
        integration.FreshnessStatus.Should().Be(GetIntegrationHealthReport.DataFreshnessStatus.Stale);
    }

    [Fact]
    public async Task GetIntegrationHealthReport_OfflineIntegration_OfflineTier()
    {
        var entry = new IIntegrationSyncReader.IntegrationSyncEntry(
            "int-3", "AzureDevOps", "AzureDevOps",
            LastSyncAt: Now.AddDays(-3),
            TotalSyncsInWindow: 10,
            SuccessfulSyncsInWindow: 0, // SyncSuccessRate = 0 → Offline
            LastErrorMessage: "Connection refused",
            IsCritical: true,
            AffectedFeatures: ["ChangeIntelligence"]);

        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integration = result.Value!.Integrations.Single();
        integration.HealthTier.Should().Be(GetIntegrationHealthReport.IntegrationHealthTier.Offline);
        result.Value.Summary.OfflineIntegrations.Should().Be(1);
        result.Value.Summary.CriticalOfflineIntegrations.Should().Contain("AzureDevOps");
    }

    [Fact]
    public async Task GetIntegrationHealthReport_MixedIntegrations_SummaryCorrect()
    {
        var entries = new[]
        {
            new IIntegrationSyncReader.IntegrationSyncEntry("i1", "GitLab", "GitLab", Now.AddMinutes(-10), 100, 99, null, false, []),
            new IIntegrationSyncReader.IntegrationSyncEntry("i2", "Jenkins", "Jenkins", Now.AddHours(-30), 50, 35, "Timeout", false, []),
            new IIntegrationSyncReader.IntegrationSyncEntry("i3", "OIDC", "OIDC", Now.AddHours(-2), 100, 80, null, true, []),
            new IIntegrationSyncReader.IntegrationSyncEntry("i4", "Kafka", "Kafka", null, 0, 0, "No sync", true, [])
        };

        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.OfflineIntegrations.Should().BeGreaterThan(0);
        result.Value.Summary.TenantIntegrationHealthScore.Should().BeLessThan(100m);
    }

    [Fact]
    public async Task GetIntegrationHealthReport_DataFreshnessImpact_PopulatedForStaleOffline()
    {
        var entry = new IIntegrationSyncReader.IntegrationSyncEntry(
            "i1", "GitLab", "GitLab",
            Now.AddHours(-60), 10, 0, "Error", true, ["ServiceCatalog", "Changes"]);

        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DataFreshnessImpact.Should().NotBeEmpty();
        result.Value.DataFreshnessImpact.Single().AffectedFeatures.Should().Contain("ServiceCatalog");
    }

    [Fact]
    public async Task GetIntegrationHealthReport_HealthHistory_Populated()
    {
        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([
                new IIntegrationSyncReader.IntegrationHealthHistoryEntry("i1", "GitLab", 0, "Healthy"),
                new IIntegrationSyncReader.IntegrationHealthHistoryEntry("i1", "GitLab", 1, "Degraded"),
                new IIntegrationSyncReader.IntegrationHealthHistoryEntry("i1", "GitLab", 2, "Healthy")
            ]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IntegrationHealthHistory.Should().HaveCount(3);
        result.Value.IntegrationHealthHistory.Any(h => h.HealthTier == GetIntegrationHealthReport.IntegrationHealthTier.Degraded)
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetIntegrationHealthReport_TopErrorIntegrations_LimitedToFive()
    {
        var entries = Enumerable.Range(1, 8).Select(i =>
            new IIntegrationSyncReader.IntegrationSyncEntry(
                $"i{i}", $"Sys{i}", "WebHook",
                Now.AddHours(-i * 5), 10, 0, $"Error {i}", false, [])).ToArray();

        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TopErrorIntegrations.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetIntegrationHealthReport_AgingFreshness_WhenSyncAgeIsOneAndHalfFreshnessHours()
    {
        var entry = new IIntegrationSyncReader.IntegrationSyncEntry(
            "i1", "Webhook", "Webhook",
            Now.AddHours(-36), 10, 9, null, false, []); // 36h > 24h but < 48h = Aging

        _reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _reader.GetHealthHistoryAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIntegrationHealthReport.Handler(_reader, _clock);
        var result = await handler.Handle(new GetIntegrationHealthReport.Query("t1", FreshnessHours: 24), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Integrations.Single().FreshnessStatus.Should().Be(GetIntegrationHealthReport.DataFreshnessStatus.Aging);
    }
}
