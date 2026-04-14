using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Infrastructure;

public sealed class RuntimeIntelligenceModuleTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCurrentHealthStatusAsync_WhenLatestSnapshotExists_ShouldReturnLatestStatus()
    {
        await using var db = CreateDbContext();
        db.RuntimeSnapshots.Add(RuntimeSnapshot.Create("orders", "production", 100m, 200m, 0.02m, 120m, 30m, 512m, 2, FixedNow.AddMinutes(-10), "test-source"));
        db.RuntimeSnapshots.Add(RuntimeSnapshot.Create("orders", "production", 110m, 1500m, 0.06m, 130m, 35m, 520m, 2, FixedNow, "test-source"));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCurrentHealthStatusAsync("orders", "production", CancellationToken.None);

        result.Should().Be("Degraded");
    }

    [Fact]
    public async Task GetCurrentHealthStatusAsync_WhenNoSnapshotExists_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetCurrentHealthStatusAsync("orders", "production", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentHealthStatusAsync_ShouldFilterByEnvironment()
    {
        await using var db = CreateDbContext();
        db.RuntimeSnapshots.Add(RuntimeSnapshot.Create("orders", "staging", 100m, 4000m, 0.12m, 120m, 30m, 512m, 2, FixedNow, "test-source"));
        db.RuntimeSnapshots.Add(RuntimeSnapshot.Create("orders", "production", 100m, 200m, 0.01m, 140m, 30m, 512m, 2, FixedNow, "test-source"));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCurrentHealthStatusAsync("orders", "production", CancellationToken.None);

        result.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetObservabilityScoreAsync_WhenNoProfile_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetObservabilityScoreAsync("orders", "production", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetObservabilityScoreAsync_WhenProfileAndBaselineExist_ShouldApplyConfidenceWeight()
    {
        await using var db = CreateDbContext();
        db.ObservabilityProfiles.Add(ObservabilityProfile.Assess("orders", "production", true, true, true, false, false, FixedNow)); // 0.70
        db.RuntimeBaselines.Add(RuntimeBaseline.Establish("orders", "production", 100m, 200m, 0.01m, 120m, FixedNow, 30, 0.80m)); // factor 0.97
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetObservabilityScoreAsync("orders", "production", CancellationToken.None);

        result.Should().Be(0.68m);
    }

    [Fact]
    public async Task GetObservabilityScoreAsync_WhenBaselineMissing_ShouldApplyMissingBaselinePenalty()
    {
        await using var db = CreateDbContext();
        db.ObservabilityProfiles.Add(ObservabilityProfile.Assess("orders", "production", true, true, true, false, false, FixedNow)); // 0.70
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetObservabilityScoreAsync("orders", "production", CancellationToken.None);

        result.Should().Be(0.63m);
    }

    [Fact]
    public async Task GetObservabilityScoreAsync_WhenOpenCriticalDriftExists_ShouldApplyCriticalPenalty()
    {
        await using var db = CreateDbContext();
        db.ObservabilityProfiles.Add(ObservabilityProfile.Assess("orders", "production", true, true, true, true, true, FixedNow)); // 1.00
        db.RuntimeBaselines.Add(RuntimeBaseline.Establish("orders", "production", 100m, 200m, 0.01m, 120m, FixedNow, 30, 1.0m)); // factor 1.00
        db.DriftFindings.Add(DriftFinding.Detect("orders", "production", "P99LatencyMs", 200m, 350m, FixedNow)); // 75% => Critical
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetObservabilityScoreAsync("orders", "production", CancellationToken.None);

        result.Should().Be(0.70m);
    }

    private static RuntimeIntelligenceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RuntimeIntelligenceDbContext>()
            .UseInMemoryDatabase($"runtime-intelligence-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new RuntimeIntelligenceDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IRuntimeIntelligenceModule CreateSut(RuntimeIntelligenceDbContext db)
        => new RuntimeIntelligenceModule(db, NullLogger<RuntimeIntelligenceModule>.Instance);

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug => "tests";
        public string Name => "Tests";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "runtime-tests-user";
        public string Name => "Runtime Tests";
        public string Email => "runtime.tests@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
