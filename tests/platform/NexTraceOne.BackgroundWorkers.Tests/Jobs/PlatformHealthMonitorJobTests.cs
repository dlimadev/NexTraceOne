#nullable disable
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

using NSubstitute;

namespace NexTraceOne.BackgroundWorkers.Tests.Jobs;

/// <summary>
/// W2-03: Testes unitários do PlatformHealthMonitorJob.
/// Verifica todas as regras de monitorização e cooldown de alertas.
/// </summary>
public sealed class PlatformHealthMonitorJobTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IPlatformHealthReader _healthReader = Substitute.For<IPlatformHealthReader>();
    private readonly WorkerJobHealthRegistry _jobRegistry = new();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly INotificationModule _notificationModule = Substitute.For<INotificationModule>();

    public PlatformHealthMonitorJobTests()
    {
        _scope.ServiceProvider.Returns(Substitute.For<IServiceProvider>());
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.GetService(typeof(INotificationModule)).Returns(_notificationModule);
    }

    private PlatformHealthMonitorJob CreateJob() =>
        new(_scopeFactory, _healthReader, _jobRegistry, _cache, NullLogger<PlatformHealthMonitorJob>.Instance);

    // ── Outbox Backlog Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task CheckOutbox_WhenPendingOver2000_SendsCriticalAlert()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(2500L);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckOutboxBacklogAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Critical" && r.Title.Contains("crítico")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckOutbox_WhenPendingOver500_SendsWarningAlert()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(750L);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckOutboxBacklogAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Warning" && r.Title.Contains("elevado")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckOutbox_WhenPendingUnder500_NoAlert()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(100L);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckOutboxBacklogAsync", CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    // ── Disk Usage Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckDisk_WhenUsageOver95Percent_SendsCriticalAlert()
    {
        _healthReader.GetPrimaryDiskUsage().Returns(new DiskUsageInfo(100_000_000_000, 96_000_000_000));
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckDiskUsageAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Critical"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckDisk_WhenUsageOver80Percent_SendsWarningAlert()
    {
        _healthReader.GetPrimaryDiskUsage().Returns(new DiskUsageInfo(100_000_000_000, 85_000_000_000));
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckDiskUsageAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Warning"),
            Arg.Any<CancellationToken>());
    }

    // ── Cooldown Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAlert_WhenCooldownActive_DoesNotSendNotification()
    {
        _healthReader.CountPendingOutboxAsync(Arg.Any<CancellationToken>()).Returns(2500L);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes("1"));

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckOutboxBacklogAsync", CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    // ── DB Pool Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckDbPool_WhenUsageOver80Percent_SendsWarningAlert()
    {
        _healthReader.GetDbPoolUsagePercentAsync(Arg.Any<CancellationToken>()).Returns(85.0);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckDbPoolAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Warning"),
            Arg.Any<CancellationToken>());
    }

    // ── Error Rate Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckErrorRate_WhenRateOver20Percent_SendsCriticalAlert()
    {
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns(25.0);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckErrorRateAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Critical"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckErrorRate_WhenRateOver5Percent_SendsWarningAlert()
    {
        _healthReader.GetErrorRatePercentAsync(Arg.Any<CancellationToken>()).Returns(10.0);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckErrorRateAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Warning"),
            Arg.Any<CancellationToken>());
    }

    // ── Stalled Jobs Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task CheckStalledJobs_WhenJobExceedsThreshold_SendsWarningAlert()
    {
        var jobName = "license-recalculation-job";
        _jobRegistry.MarkStarted(jobName);
        // Simular falha - nunca marcar como succeeded

        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "CheckStalledJobsAsync", CancellationToken.None);

        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.Severity == "Warning" && r.Title.Contains("não executa")),
            Arg.Any<CancellationToken>());
    }

    // ── Helper Methods ───────────────────────────────────────────────────────────

    private static async Task InvokePrivateMethod(object obj, string methodName, CancellationToken cancellationToken)
    {
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method is null)
            throw new InvalidOperationException($"Método '{methodName}' não encontrado.");

        var task = (Task)method.Invoke(obj, new object[] { cancellationToken })!;
        await task;
    }
}
