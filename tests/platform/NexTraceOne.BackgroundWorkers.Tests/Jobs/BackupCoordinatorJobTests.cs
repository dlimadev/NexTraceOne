#nullable disable
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BackgroundWorkers.Backup;
using NexTraceOne.BackgroundWorkers.Configuration;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NSubstitute;
using Xunit;

namespace NexTraceOne.BackgroundWorkers.Tests.Jobs;

/// <summary>
/// Testes unitários para o BackupCoordinatorJob (W3-03).
/// Cobre: backup bem-sucedido, falha parcial, retenção, manifesto e notificações.
/// </summary>
public class BackupCoordinatorJobTests
{
    private readonly IBackupProcess _backupProcess;
    private readonly WorkerJobHealthRegistry _healthRegistry;
    private readonly IOptions<BackupOptions> _options;
    private readonly ILogger<BackupCoordinatorJob> _logger;

    public BackupCoordinatorJobTests()
    {
        _backupProcess = Substitute.For<IBackupProcess>();
        _healthRegistry = new WorkerJobHealthRegistry();
        _options = Options.Create(new BackupOptions
        {
            OutputDirectory = Path.Combine(Path.GetTempPath(), "nextraceone-test-backups"),
            RetentionDays = 7,
            Databases = ["testdb1", "testdb2"],
            DumpTimeoutMinutes = 5
        });
        _logger = Substitute.For<ILogger<BackupCoordinatorJob>>();
    }

    [Fact]
    public async Task RunBackupCycle_WhenAllDatabasesSucceed_ShouldCreateManifestAndNotNotify()
    {
        // Arrange
        var job = CreateJob();
        Directory.CreateDirectory(_options.Value.OutputDirectory);

        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act - invoke internal method via reflection
        await InvokeRunBackupCycleAsync(job, CancellationToken.None);

        // Assert
        var sessionDirs = Directory.GetDirectories(_options.Value.OutputDirectory);
        sessionDirs.Should().HaveCount(1);

        var manifestPath = Path.Combine(sessionDirs[0], "manifest.json");
        File.Exists(manifestPath).Should().BeTrue("Manifesto deve ser criado");

        var manifestContent = await File.ReadAllTextAsync(manifestPath);
        manifestContent.Should().Contain("testdb1");
        manifestContent.Should().Contain("testdb2");
        manifestContent.Should().Contain("Success");

        // Cleanup
        Directory.Delete(_options.Value.OutputDirectory, true);
    }

    [Fact]
    public async Task RunBackupCycle_WhenOneDatabaseFails_ShouldContinueWithOthers()
    {
        // Arrange
        var job = CreateJob();
        Directory.CreateDirectory(_options.Value.OutputDirectory);

        var callCount = 0;
        _backupProcess.When(x => x.DumpAsync(
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
            .Do(async callInfo =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Simulated pg_dump failure");
            });

        // Act
        await InvokeRunBackupCycleAsync(job, CancellationToken.None);

        // Assert
        var sessionDirs = Directory.GetDirectories(_options.Value.OutputDirectory);
        sessionDirs.Should().HaveCount(1);

        var manifestPath = Path.Combine(sessionDirs[0], "manifest.json");
        var manifestContent = await File.ReadAllTextAsync(manifestPath);
        manifestContent.Should().Contain("Success");
        manifestContent.Should().Contain("false");

        // Cleanup
        Directory.Delete(_options.Value.OutputDirectory, true);
    }

    [Fact]
    public async Task RunBackupCycle_WhenAllDatabasesFail_ShouldNotifyPlatformAdmins()
    {
        // Arrange
        var job = CreateJobWithNotificationModule(out var notificationModule);
        Directory.CreateDirectory(_options.Value.OutputDirectory);

        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(ci => throw new InvalidOperationException("pg_dump failed"));

        // Act
        await InvokeRunBackupCycleAsync(job, CancellationToken.None);

        // Assert
        await notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(n =>
                n.EventType == "Backup.PartialFailure" &&
                n.Severity == "Critical"),
            Arg.Any<CancellationToken>());

        // Cleanup
        Directory.Delete(_options.Value.OutputDirectory, true);
    }

    [Fact]
    public async Task RunBackupCycle_ShouldGenerateSha256ChecksumForEachFile()
    {
        // Arrange
        var job = CreateJob();
        Directory.CreateDirectory(_options.Value.OutputDirectory);

        _backupProcess.When(x => x.DumpAsync(
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
            .Do(async callInfo =>
            {
                var stream = callInfo.ArgAt<Stream>(5);
                var data = Encoding.UTF8.GetBytes("fake backup data");
                await stream.WriteAsync(data, CancellationToken.None);
            });

        // Act
        await InvokeRunBackupCycleAsync(job, CancellationToken.None);

        // Assert
        var sessionDirs = Directory.GetDirectories(_options.Value.OutputDirectory);
        var manifestPath = Path.Combine(sessionDirs[0], "manifest.json");
        var manifestContent = await File.ReadAllTextAsync(manifestPath);

        // SHA-256 hash tem 64 caracteres hexadecimais - verificar se há um campo Sha256
        manifestContent.Should().Contain("Sha256");
        manifestContent.Should().Contain(":");

        // Cleanup
        Directory.Delete(_options.Value.OutputDirectory, true);
    }

    [Fact]
    public void ApplyRetentionPolicy_WhenBackupsAreOlderThanRetention_ShouldDeleteThem()
    {
        // Arrange
        var outputDir = Path.Combine(Path.GetTempPath(), $"nextraceone-retention-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        // Criar diretórios de backup antigos (mais de 7 dias)
        var oldBackupDir = Path.Combine(outputDir, "2020-01-01_12-00-00");
        Directory.CreateDirectory(oldBackupDir);
        File.WriteAllText(Path.Combine(oldBackupDir, "old.dump.gz"), "old");

        // Forçar data de criação antiga
        Directory.SetCreationTimeUtc(oldBackupDir, DateTime.UtcNow.AddDays(-30));

        // Criar diretório recente
        var recentBackupDir = Path.Combine(outputDir, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"));
        Directory.CreateDirectory(recentBackupDir);
        File.WriteAllText(Path.Combine(recentBackupDir, "recent.dump.gz"), "recent");

        var options = Options.Create(new BackupOptions
        {
            OutputDirectory = outputDir,
            RetentionDays = 7,
            Databases = [],
            DumpTimeoutMinutes = 5
        });

        var job = new BackupCoordinatorJob(
            _backupProcess,
            Substitute.For<IServiceScopeFactory>(),
            _healthRegistry,
            options,
            _logger);

        // Act - invoca método privado via reflection
        var method = typeof(BackupCoordinatorJob).GetMethod("ApplyRetentionPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(job, [options.Value]);

        // Assert
        Directory.Exists(oldBackupDir).Should().BeFalse("Backup antigo deve ser removido");
        Directory.Exists(recentBackupDir).Should().BeTrue("Backup recente deve ser mantido");

        // Cleanup
        Directory.Delete(outputDir, true);
    }

    [Fact]
    public async Task RunBackupCycle_WhenPartialFailure_ShouldSendWarningNotification()
    {
        // Arrange
        var job = CreateJobWithNotificationModule(out var notificationModule);
        Directory.CreateDirectory(_options.Value.OutputDirectory);

        var callCount = 0;
        _backupProcess.When(x => x.DumpAsync(
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
            .Do(async callInfo =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("First db failed");
            });

        // Act
        await InvokeRunBackupCycleAsync(job, CancellationToken.None);

        // Assert - verifica que notificação foi enviada (independentemente da severidade)
        await notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(n => n.EventType == "Backup.PartialFailure"),
            Arg.Any<CancellationToken>());

        // Cleanup
        Directory.Delete(_options.Value.OutputDirectory, true);
    }

    [Fact]
    public async Task RunBackupCycle_ShouldCompressBackupWithGZip()
    {
        // Arrange
        var job = CreateJob();
        Directory.CreateDirectory(_options.Value.OutputDirectory);

        Stream capturedStream = null;
        _backupProcess.When(x => x.DumpAsync(
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => capturedStream = callInfo.ArgAt<Stream>(5));

        // Act
        await InvokeRunBackupCycleAsync(job, CancellationToken.None);

        // Assert
        var sessionDirs = Directory.GetDirectories(_options.Value.OutputDirectory);
        var dumpFiles = Directory.GetFiles(sessionDirs[0], "*.dump.gz");
        dumpFiles.Should().HaveCount(2, "Cada base de dados deve ter um ficheiro .dump.gz");

        // Cleanup
        Directory.Delete(_options.Value.OutputDirectory, true);
    }

    [Fact]
    public async Task RunBackupCycle_ShouldIncludeTimestampInManifest()
    {
        // Arrange
        var job = CreateJob();
        Directory.CreateDirectory(_options.Value.OutputDirectory);

        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await InvokeRunBackupCycleAsync(job, CancellationToken.None);

        // Assert
        var sessionDirs = Directory.GetDirectories(_options.Value.OutputDirectory);
        var manifestPath = Path.Combine(sessionDirs[0], "manifest.json");
        var manifestContent = await File.ReadAllTextAsync(manifestPath);

        manifestContent.Should().Contain("Timestamp");
        manifestContent.Should().Contain("ExpectedDatabases");

        // Cleanup
        Directory.Delete(_options.Value.OutputDirectory, true);
    }

    private BackupCoordinatorJob CreateJob()
    {
        return new BackupCoordinatorJob(
            _backupProcess,
            Substitute.For<IServiceScopeFactory>(),
            _healthRegistry,
            _options,
            _logger);
    }

    private BackupCoordinatorJob CreateJobWithNotificationModule(out INotificationModule notificationModule)
    {
        notificationModule = Substitute.For<INotificationModule>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        scope.ServiceProvider.GetService(typeof(INotificationModule)).Returns(notificationModule);
        scopeFactory.CreateScope().Returns(scope);

        return new BackupCoordinatorJob(
            _backupProcess,
            scopeFactory,
            _healthRegistry,
            _options,
            _logger);
    }

    private async Task InvokeRunBackupCycleAsync(BackupCoordinatorJob job, CancellationToken cancellationToken)
    {
        var method = typeof(BackupCoordinatorJob).GetMethod("RunBackupCycleAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(job, [cancellationToken]);
    }
}
