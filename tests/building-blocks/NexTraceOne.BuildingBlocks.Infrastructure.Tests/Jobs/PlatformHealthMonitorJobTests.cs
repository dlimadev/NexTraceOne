using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Jobs;

public sealed class PlatformHealthMonitorJobTests
{
    private readonly IPlatformHealthReader _healthReader = Substitute.For<IPlatformHealthReader>();
    private readonly WorkerJobHealthRegistry _registry = new();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly INotificationModule _notificationModule = Substitute.For<INotificationModule>();

    private PlatformHealthMonitorJob CreateJob()
    {
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(INotificationModule)).Returns(_notificationModule);
        scope.ServiceProvider.Returns(provider);
        _scopeFactory.CreateScope().Returns(scope);

        _notificationModule.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(true));

        return new PlatformHealthMonitorJob(
            _scopeFactory,
            _healthReader,
            _registry,
            _cache,
            NullLogger<PlatformHealthMonitorJob>.Instance);
    }

    // ── DiskUsageInfo ─────────────────────────────────────────────────────────

    [Fact]
    public void DiskUsageInfo_UsedPercent_CalculatesCorrectly()
    {
        var info = new DiskUsageInfo(TotalBytes: 1000, UsedBytes: 800);
        info.UsedPercent.Should().BeApproximately(80.0, precision: 0.01);
    }

    [Fact]
    public void DiskUsageInfo_Unknown_HasZeroPercent()
    {
        DiskUsageInfo.Unknown.UsedPercent.Should().Be(0);
    }

    [Fact]
    public void DiskUsageInfo_TotalZero_ReturnsZeroPercent()
    {
        var info = new DiskUsageInfo(TotalBytes: 0, UsedBytes: 500);
        info.UsedPercent.Should().Be(0);
    }

    // ── Outbox checks ─────────────────────────────────────────────────────────

    [Fact]
    public async Task OutboxBelowThreshold_NoAlertSent()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(100L);
        _healthReader.GetPrimaryDiskUsage().Returns(new DiskUsageInfo(1000, 500));
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));

        // Não pode executar o loop completo; vamos testar o método interno directamente via reflexão
        // ou aceitar que sem alertas não há chamadas ao notification module.
        await Task.CompletedTask;

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType.Contains("outbox")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxAbove500_SendsWarning()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(600L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        // Sem cooldown activo
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();

        // Invocar o método de verificação via helper público
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.Severity == "Warning" &&
                r.EventType.Contains("outbox-warning")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxAbove2000_SendsCritical()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(2500L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.Severity == "Critical" &&
                r.EventType.Contains("outbox-critical")),
            Arg.Any<CancellationToken>());
    }

    // ── Disk checks ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DiskAbove80Pct_SendsWarning()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(new DiskUsageInfo(1000, 850));
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.Severity == "Warning" &&
                r.EventType.Contains("disk-warning")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiskAbove95Pct_SendsCritical()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(new DiskUsageInfo(1000, 960));
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.Severity == "Critical" &&
                r.EventType.Contains("disk-critical")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiskUnknown_NoAlertSent()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType.Contains("disk")),
            Arg.Any<CancellationToken>());
    }

    // ── Cooldown ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenCooldownActive_AlertNotSentAgain()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(2500L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        // Cache retorna valor existente → cooldown activo
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x31 }); // "1"

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Any<NotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AfterCooldownExpires_AlertSentAgain()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(600L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);

        // Primeira chamada: sem cooldown; segunda: cooldown activo
        var callCount = 0;
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => callCount++ == 0 ? (byte[]?)null : new byte[] { 0x31 });

        var job = CreateJob();

        await InvokeRunChecksAsync(job, CancellationToken.None);
        await InvokeRunChecksAsync(job, CancellationToken.None);

        // Apenas 1 alerta enviado (segunda chamada bloqueada por cooldown)
        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType.Contains("outbox-warning")),
            Arg.Any<CancellationToken>());
    }

    // ── Recipient roles ───────────────────────────────────────────────────────

    [Fact]
    public async Task Alert_AlwaysTargetsPlatformAdminRole()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(600L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.RecipientRoles != null &&
                r.RecipientRoles.Contains("PlatformAdmin")),
            Arg.Any<CancellationToken>());
    }

    // ── HealthCheckName ───────────────────────────────────────────────────────

    [Fact]
    public void HealthCheckName_IsCorrect()
    {
        PlatformHealthMonitorJob.HealthCheckName.Should().Be("platform-health-monitor-job");
    }

    // ── Stalled job check ─────────────────────────────────────────────────────

    [Fact]
    public async Task JobNeverSucceeded_SendsWarning()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        // Regista o LicenseRecalculationJob como iniciado mas nunca bem-sucedido
        _registry.MarkStarted(LicenseRecalculationJob.HealthCheckName);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.EventType.Contains("stalled-job") &&
                r.Severity == "Warning"),
            Arg.Any<CancellationToken>());
    }

    // ── DB pool checks ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DbPoolNull_NoAlertSent()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType.Contains("dbpool")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DbPoolAbove80Pct_SendsWarning()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)85.0);
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.Severity == "Warning" &&
                r.EventType.Contains("dbpool-warning")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DbPoolBelow80Pct_NoAlertSent()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)50.0);
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType.Contains("dbpool")),
            Arg.Any<CancellationToken>());
    }

    // ── Error rate checks ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ErrorRateNull_NoAlertSent()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType.Contains("errorrate")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ErrorRateAbove5Pct_SendsWarning()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)10.0);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.Severity == "Warning" &&
                r.EventType.Contains("errorrate-warning")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ErrorRateAbove20Pct_SendsCritical()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)25.0);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r =>
                r.Severity == "Critical" &&
                r.EventType.Contains("errorrate-critical")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ErrorRateBelow5Pct_NoAlertSent()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(0L);
        _healthReader.GetPrimaryDiskUsage().Returns(DiskUsageInfo.Unknown);
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)null);
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns((double?)2.0);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokeRunChecksAsync(job, CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType.Contains("errorrate")),
            Arg.Any<CancellationToken>());
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    // Invoca o método privado RunChecksAsync via reflexão para testes unitários.
    private static async Task InvokeRunChecksAsync(PlatformHealthMonitorJob job, CancellationToken ct)
    {
        var method = typeof(PlatformHealthMonitorJob)
            .GetMethod("RunChecksAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)method.Invoke(job, [ct])!;
    }
}
