using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Backup;
using NexTraceOne.BackgroundWorkers.Configuration;
using NexTraceOne.BackgroundWorkers.Jobs;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Jobs;

public sealed class BackupCoordinatorJobTests : IDisposable
{
    private readonly IBackupProcess _backupProcess = Substitute.For<IBackupProcess>();
    private readonly WorkerJobHealthRegistry _registry = new();
    private readonly string _tempDir;

    public BackupCoordinatorJobTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private BackupCoordinatorJob CreateJob(BackupOptions? opts = null)
    {
        opts ??= new BackupOptions
        {
            OutputDirectory = _tempDir,
            RetentionDays = 7,
            Databases = ["db_test"],
            DumpTimeoutMinutes = 5,
        };

        return new BackupCoordinatorJob(
            _backupProcess,
            _registry,
            Options.Create(opts),
            NullLogger<BackupCoordinatorJob>.Instance);
    }

    [Fact]
    public void HealthCheckName_IsCorrect()
    {
        BackupCoordinatorJob.HealthCheckName.Should().Be("backup-coordinator-job");
    }

    [Fact]
    public async Task RunBackupCycle_WhenDumpSucceeds_CreatesOutputDirectory()
    {
        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = CreateJob();
        await job.RunBackupCycleAsync(CancellationToken.None);

        var sessionDirs = Directory.GetDirectories(_tempDir);
        sessionDirs.Should().HaveCount(1, "deve criar um directório por ciclo de backup");
    }

    [Fact]
    public async Task RunBackupCycle_WhenDumpSucceeds_CreatesManifest()
    {
        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = CreateJob();
        await job.RunBackupCycleAsync(CancellationToken.None);

        var sessionDir = Directory.GetDirectories(_tempDir).Single();
        var manifestPath = Path.Combine(sessionDir, "manifest.json");
        File.Exists(manifestPath).Should().BeTrue("o manifesto deve ser criado após o backup");
    }

    [Fact]
    public async Task RunBackupCycle_ManifestContainsExpectedDatabase()
    {
        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = CreateJob();
        await job.RunBackupCycleAsync(CancellationToken.None);

        var sessionDir = Directory.GetDirectories(_tempDir).Single();
        var manifestJson = await File.ReadAllTextAsync(Path.Combine(sessionDir, "manifest.json"));
        manifestJson.Should().Contain("db_test");
    }

    [Fact]
    public async Task RunBackupCycle_WhenDumpFails_ContinuesWithOtherDatabases()
    {
        var opts = new BackupOptions
        {
            OutputDirectory = _tempDir,
            Databases = ["db_fail", "db_ok"],
            DumpTimeoutMinutes = 5,
        };

        var callCount = 0;
        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (callCount++ == 0) throw new InvalidOperationException("pg_dump falhou");
                return Task.CompletedTask;
            });

        var job = CreateJob(opts);

        // Não deve lançar exceção — falhas individuais são toleradas
        await job.RunBackupCycleAsync(CancellationToken.None);

        var sessionDir = Directory.GetDirectories(_tempDir).Single();
        var manifestJson = await File.ReadAllTextAsync(Path.Combine(sessionDir, "manifest.json"));

        manifestJson.Should().Contain("db_fail");
        manifestJson.Should().Contain("db_ok");
    }

    [Fact]
    public async Task RunBackupCycle_CreatesCompressedFile()
    {
        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = CreateJob();
        await job.RunBackupCycleAsync(CancellationToken.None);

        var sessionDir = Directory.GetDirectories(_tempDir).Single();
        var dumpFiles = Directory.GetFiles(sessionDir, "*.dump.gz");
        dumpFiles.Should().HaveCount(1, "deve criar um ficheiro .dump.gz por base de dados");
    }

    [Fact]
    public async Task RunBackupCycle_AppliesRetentionPolicy()
    {
        // Cria um directório "antigo" que deve ser removido
        var oldDir = Path.Combine(_tempDir, "2020-01-01_00-00-00");
        Directory.CreateDirectory(oldDir);
        Directory.SetCreationTimeUtc(oldDir, DateTime.UtcNow.AddDays(-100));

        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = CreateJob(new BackupOptions
        {
            OutputDirectory = _tempDir,
            RetentionDays = 7,
            Databases = ["db_test"],
            DumpTimeoutMinutes = 5,
        });

        await job.RunBackupCycleAsync(CancellationToken.None);

        Directory.Exists(oldDir).Should().BeFalse("directórios mais antigos que RetentionDays devem ser removidos");
    }

    [Fact]
    public async Task RunBackupCycle_WhenMultipleDatabases_DumpsEachOne()
    {
        var opts = new BackupOptions
        {
            OutputDirectory = _tempDir,
            Databases = ["db1", "db2", "db3"],
            DumpTimeoutMinutes = 5,
        };

        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = CreateJob(opts);
        await job.RunBackupCycleAsync(CancellationToken.None);

        await _backupProcess.Received(3).DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunBackupCycle_ManifestEntryHasSha256()
    {
        _backupProcess.DumpAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = CreateJob();
        await job.RunBackupCycleAsync(CancellationToken.None);

        var sessionDir = Directory.GetDirectories(_tempDir).Single();
        var manifestJson = await File.ReadAllTextAsync(Path.Combine(sessionDir, "manifest.json"));
        var doc = JsonDocument.Parse(manifestJson);
        var entries = doc.RootElement.GetProperty("Entries");

        entries.EnumerateArray().Should().AllSatisfy(entry =>
        {
            var sha = entry.GetProperty("Sha256").GetString();
            sha.Should().NotBeNullOrEmpty("todos os backups bem-sucedidos devem ter hash SHA-256");
        });
    }
}
