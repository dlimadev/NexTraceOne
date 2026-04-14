using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Services;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Infrastructure;

public sealed class ReliabilityModuleServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TestTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    // ── GetCurrentReliabilityStatusAsync ──────────────────────────────────

    [Fact]
    public async Task GetCurrentReliabilityStatusAsync_WhenSnapshotExists_ShouldReturnStatus()
    {
        await using var db = CreateDbContext();
        db.ReliabilitySnapshots.Add(ReliabilitySnapshot.Create(
            TestTenantId, "orders", "production", 85m, 90m, 80m, 75m, 1,
            "Healthy", TrendDirection.Stable, FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCurrentReliabilityStatusAsync("orders", "production");

        result.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetCurrentReliabilityStatusAsync_WhenNoSnapshot_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetCurrentReliabilityStatusAsync("unknown", "production");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentReliabilityStatusAsync_ShouldReturnLatestSnapshot()
    {
        await using var db = CreateDbContext();
        db.ReliabilitySnapshots.Add(ReliabilitySnapshot.Create(
            TestTenantId, "orders", "production", 70m, 60m, 80m, 70m, 2,
            "Degraded", TrendDirection.Declining, FixedNow.AddHours(-1)));
        db.ReliabilitySnapshots.Add(ReliabilitySnapshot.Create(
            TestTenantId, "orders", "production", 90m, 95m, 85m, 80m, 0,
            "Healthy", TrendDirection.Improving, FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCurrentReliabilityStatusAsync("orders", "production");

        result.Should().Be("Healthy");
    }

    // ── GetRemainingErrorBudgetAsync ──────────────────────────────────────

    [Fact]
    public async Task GetRemainingErrorBudgetAsync_WhenBudgetExists_ShouldReturnFraction()
    {
        await using var db = CreateDbContext();

        var slo = SloDefinition.Create(TestTenantId, "orders", "production",
            "Availability SLO", SloType.Availability, 99.9m, 30);
        db.SloDefinitions.Add(slo);

        var budget = ErrorBudgetSnapshot.Create(TestTenantId, slo.Id,
            "orders", "production", 43.2m, 10.8m, FixedNow);
        db.ErrorBudgetSnapshots.Add(budget);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetRemainingErrorBudgetAsync("orders", "production");

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(0.75m, 0.01m); // 32.4 remaining / 43.2 total
    }

    [Fact]
    public async Task GetRemainingErrorBudgetAsync_WhenNoSlo_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetRemainingErrorBudgetAsync("orders", "production");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRemainingErrorBudgetAsync_WhenSloDeactivated_ShouldReturnNull()
    {
        await using var db = CreateDbContext();

        var slo = SloDefinition.Create(TestTenantId, "orders", "production",
            "Availability SLO", SloType.Availability, 99.9m, 30);
        slo.Deactivate();
        db.SloDefinitions.Add(slo);

        var budget = ErrorBudgetSnapshot.Create(TestTenantId, slo.Id,
            "orders", "production", 43.2m, 10.8m, FixedNow);
        db.ErrorBudgetSnapshots.Add(budget);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetRemainingErrorBudgetAsync("orders", "production");

        result.Should().BeNull();
    }

    // ── GetCurrentBurnRateAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetCurrentBurnRateAsync_WhenSnapshotExists_ShouldReturnBurnRate()
    {
        await using var db = CreateDbContext();

        var slo = SloDefinition.Create(TestTenantId, "orders", "production",
            "Availability SLO", SloType.Availability, 99.9m, 30);
        db.SloDefinitions.Add(slo);

        var burnRate = BurnRateSnapshot.Create(TestTenantId, slo.Id,
            "orders", "production", BurnRateWindow.OneHour, 0.002m, 0.001m, FixedNow);
        db.BurnRateSnapshots.Add(burnRate);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCurrentBurnRateAsync("orders", "production");

        result.Should().NotBeNull();
        result!.Value.Should().Be(2.0m); // 0.002 / 0.001 = 2.0
    }

    [Fact]
    public async Task GetCurrentBurnRateAsync_WhenNoSnapshot_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetCurrentBurnRateAsync("orders", "production");

        result.Should().BeNull();
    }

    // ── GetServiceSlosAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetServiceSlosAsync_WhenSlosExist_ShouldReturnSummaries()
    {
        await using var db = CreateDbContext();
        db.SloDefinitions.Add(SloDefinition.Create(TestTenantId, "orders", "production",
            "Availability SLO", SloType.Availability, 99.9m, 30));
        db.SloDefinitions.Add(SloDefinition.Create(TestTenantId, "orders", "production",
            "Latency SLO", SloType.Latency, 95.0m, 30));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetServiceSlosAsync("orders", "production");

        result.Should().HaveCount(2);
        result[0].SloType.Should().Be("Availability");
        result[1].SloType.Should().Be("Latency");
        result.Should().OnlyContain(s => s.Status == "Active");
    }

    [Fact]
    public async Task GetServiceSlosAsync_WhenNoSlos_ShouldReturnEmpty()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetServiceSlosAsync("orders", "production");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetServiceSlosAsync_ShouldExcludeInactiveSlos()
    {
        await using var db = CreateDbContext();
        var activeSlo = SloDefinition.Create(TestTenantId, "orders", "production",
            "Availability SLO", SloType.Availability, 99.9m, 30);
        var inactiveSlo = SloDefinition.Create(TestTenantId, "orders", "production",
            "Old SLO", SloType.ErrorRate, 99.0m, 7);
        inactiveSlo.Deactivate();
        db.SloDefinitions.AddRange(activeSlo, inactiveSlo);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetServiceSlosAsync("orders", "production");

        result.Should().ContainSingle();
        result[0].SloType.Should().Be("Availability");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ReliabilityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ReliabilityDbContext>()
            .UseInMemoryDatabase($"reliability-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new ReliabilityDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IReliabilityModule CreateSut(ReliabilityDbContext db) => new ReliabilityModuleService(db);

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => TestTenantId;
        public string Slug => "tests";
        public string Name => "Tests";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "reliability-tests-user";
        public string Name => "Reliability Tests";
        public string Email => "reliability.tests@nextraceone.local";
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
