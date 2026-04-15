using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Services;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Infrastructure;

public sealed class CostIntelligenceModuleServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TestBatchId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ── GetCurrentMonthlyCostAsync ───────────────────────────────────────

    [Fact]
    public async Task GetCurrentMonthlyCostAsync_WhenProfileExists_ShouldReturnCurrentCost()
    {
        await using var db = CreateDbContext();
        var profile = ServiceCostProfile.Create("orders", "production", 80m, FixedNow, 5000m);
        profile.UpdateCurrentCost(3250.50m, FixedNow);
        db.ServiceCostProfiles.Add(profile);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCurrentMonthlyCostAsync("orders", "production", CancellationToken.None);

        result.Should().Be(3250.50m);
    }

    [Fact]
    public async Task GetCurrentMonthlyCostAsync_WhenNoProfile_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetCurrentMonthlyCostAsync("orders", "production", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentMonthlyCostAsync_ShouldFilterByEnvironment()
    {
        await using var db = CreateDbContext();
        var staging = ServiceCostProfile.Create("orders", "staging", 80m, FixedNow, 2000m);
        staging.UpdateCurrentCost(1800m, FixedNow);
        var production = ServiceCostProfile.Create("orders", "production", 80m, FixedNow, 5000m);
        production.UpdateCurrentCost(4200m, FixedNow);
        db.ServiceCostProfiles.AddRange(staging, production);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCurrentMonthlyCostAsync("orders", "production", CancellationToken.None);

        result.Should().Be(4200m);
    }

    // ── GetCostTrendPercentageAsync ──────────────────────────────────────

    [Fact]
    public async Task GetCostTrendPercentageAsync_WhenTrendExists_ShouldReturnLatestPercentageChange()
    {
        await using var db = CreateDbContext();
        var older = CostTrend.Create("orders", "production",
            FixedNow.AddDays(-60), FixedNow.AddDays(-30), 100m, 150m, 8m, 30);
        var latest = CostTrend.Create("orders", "production",
            FixedNow.AddDays(-30), FixedNow, 110m, 170m, 12.5m, 30);
        db.CostTrends.Add(older.Value);
        db.CostTrends.Add(latest.Value);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostTrendPercentageAsync("orders", "production", CancellationToken.None);

        result.Should().Be(12.5m);
    }

    [Fact]
    public async Task GetCostTrendPercentageAsync_WhenNoTrend_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetCostTrendPercentageAsync("orders", "production", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCostTrendPercentageAsync_ShouldFilterByServiceAndEnvironment()
    {
        await using var db = CreateDbContext();
        var ordersTrend = CostTrend.Create("orders", "production",
            FixedNow.AddDays(-30), FixedNow, 100m, 150m, 15m, 30);
        var paymentsTrend = CostTrend.Create("payments", "production",
            FixedNow.AddDays(-30), FixedNow, 200m, 250m, -3m, 30);
        db.CostTrends.AddRange(ordersTrend.Value, paymentsTrend.Value);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostTrendPercentageAsync("orders", "production", CancellationToken.None);

        result.Should().Be(15m);
    }

    // ── GetCostRecordsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetCostRecordsAsync_WhenRecordsExist_ShouldReturnAllRecords()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m));
        db.CostRecords.Add(CreateCostRecord("svc-payments", "Payments", "TeamB", "Finance", "production", "2026-03", 800m));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostRecordsAsync(cancellationToken: CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].TotalCost.Should().BeGreaterThanOrEqualTo(result[1].TotalCost);
    }

    [Fact]
    public async Task GetCostRecordsAsync_WithPeriodFilter_ShouldReturnFilteredRecords()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-02", 900m));
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostRecordsAsync("2026-03", CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Period.Should().Be("2026-03");
    }

    [Fact]
    public async Task GetCostRecordsAsync_WhenNoRecords_ShouldReturnEmpty()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetCostRecordsAsync(cancellationToken: CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── GetServiceCostAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetServiceCostAsync_WhenRecordExists_ShouldReturnLatestRecord()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-02", 900m, FixedNow.AddDays(-30)));
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m, FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetServiceCostAsync("svc-orders", cancellationToken: CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalCost.Should().Be(1200m);
        result.Period.Should().Be("2026-03");
    }

    [Fact]
    public async Task GetServiceCostAsync_WithPeriodFilter_ShouldReturnRecordForPeriod()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-02", 900m, FixedNow.AddDays(-30)));
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m, FixedNow));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetServiceCostAsync("svc-orders", "2026-02", CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalCost.Should().Be(900m);
    }

    [Fact]
    public async Task GetServiceCostAsync_WhenNoRecord_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetServiceCostAsync("svc-orders", cancellationToken: CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetCostsByTeamAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetCostsByTeamAsync_ShouldReturnRecordsForTeam()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m));
        db.CostRecords.Add(CreateCostRecord("svc-inventory", "Inventory", "TeamA", "Commerce", "production", "2026-03", 600m));
        db.CostRecords.Add(CreateCostRecord("svc-payments", "Payments", "TeamB", "Finance", "production", "2026-03", 800m));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostsByTeamAsync("TeamA", cancellationToken: CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.Team == "TeamA");
    }

    [Fact]
    public async Task GetCostsByTeamAsync_WithPeriodFilter_ShouldReturnFilteredRecords()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-02", 900m));
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostsByTeamAsync("TeamA", "2026-03", CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Period.Should().Be("2026-03");
    }

    // ── GetCostsByDomainAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetCostsByDomainAsync_ShouldReturnRecordsForDomain()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m));
        db.CostRecords.Add(CreateCostRecord("svc-inventory", "Inventory", "TeamA", "Commerce", "production", "2026-03", 600m));
        db.CostRecords.Add(CreateCostRecord("svc-payments", "Payments", "TeamB", "Finance", "production", "2026-03", 800m));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostsByDomainAsync("Commerce", cancellationToken: CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.Domain == "Commerce");
    }

    [Fact]
    public async Task GetCostsByDomainAsync_WithPeriodFilter_ShouldReturnFilteredRecords()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-02", 900m));
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostsByDomainAsync("Commerce", "2026-03", CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Period.Should().Be("2026-03");
    }

    [Fact]
    public async Task GetCostsByDomainAsync_WhenNoDomainMatch_ShouldReturnEmpty()
    {
        await using var db = CreateDbContext();
        db.CostRecords.Add(CreateCostRecord("svc-orders", "Orders", "TeamA", "Commerce", "production", "2026-03", 1200m));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetCostsByDomainAsync("NonExistent", cancellationToken: CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static CostRecord CreateCostRecord(
        string serviceId, string serviceName, string? team, string? domain,
        string? environment, string period, decimal totalCost,
        DateTimeOffset? recordedAt = null)
    {
        var result = CostRecord.Create(
            TestBatchId, serviceId, serviceName, team, domain, environment,
            period, totalCost, "USD", "test-source", recordedAt ?? FixedNow);
        return result.Value;
    }

    private static CostIntelligenceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CostIntelligenceDbContext>()
            .UseInMemoryDatabase($"cost-intelligence-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new CostIntelligenceDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static ICostIntelligenceModule CreateSut(CostIntelligenceDbContext db)
        => new CostIntelligenceModuleService(db, NullLogger<CostIntelligenceModuleService>.Instance);

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
        public string Id => "cost-tests-user";
        public string Name => "Cost Tests";
        public string Email => "cost.tests@nextraceone.local";
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
