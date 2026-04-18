using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Contracts;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence;
using NexTraceOne.ProductAnalytics.Infrastructure.Services;

namespace NexTraceOne.ProductAnalytics.Tests.Infrastructure.Services;

/// <summary>
/// Testes de unidade para <see cref="ProductAnalyticsModuleService"/>
/// — contrato cross-module <see cref="IProductAnalyticsModule"/>.
/// </summary>
public sealed class ProductAnalyticsModuleServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 30, 14, 0, 0, TimeSpan.Zero);

    // ── GetModuleEventCountAsync ──

    [Fact]
    public async Task GetModuleEventCount_WithMatchingEvents_ShouldReturnCount()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        await SeedEventsAsync(db,
            CreateEvent(ProductModule.ServiceCatalog),
            CreateEvent(ProductModule.ServiceCatalog),
            CreateEvent(ProductModule.ContractStudio));

        var count = await sut.GetModuleEventCountAsync(
            "ServiceCatalog",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetModuleEventCount_WithNoMatchingEvents_ShouldReturnZero()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        await SeedEventsAsync(db, CreateEvent(ProductModule.ContractStudio));

        var count = await sut.GetModuleEventCountAsync(
            "ServiceCatalog",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetModuleEventCount_WithInvalidModuleName_ShouldReturnZero()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        var count = await sut.GetModuleEventCountAsync(
            "NonExistentModule",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetModuleEventCount_WithEmptyName_ShouldReturnZero()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        var count = await sut.GetModuleEventCountAsync(
            "",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetModuleEventCount_ShouldBeCaseInsensitive()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        await SeedEventsAsync(db, CreateEvent(ProductModule.ServiceCatalog));

        var count = await sut.GetModuleEventCountAsync(
            "servicecatalog",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task GetModuleEventCount_EventsOutsideRange_ShouldReturnZero()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        await SeedEventsAsync(db, CreateEvent(ProductModule.ServiceCatalog));

        var count = await sut.GetModuleEventCountAsync(
            "ServiceCatalog",
            FixedNow.AddDays(-10),
            FixedNow.AddDays(-5),
            CancellationToken.None);

        count.Should().Be(0);
    }

    // ── GetActivePersonasAsync ──

    [Fact]
    public async Task GetActivePersonas_ShouldReturnDistinctPersonas()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        await SeedEventsAsync(db,
            CreateEvent(ProductModule.ServiceCatalog, persona: "Engineer"),
            CreateEvent(ProductModule.ServiceCatalog, persona: "Engineer"),
            CreateEvent(ProductModule.ContractStudio, persona: "Architect"),
            CreateEvent(ProductModule.Dashboard, persona: null));

        var personas = await sut.GetActivePersonasAsync(
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        personas.Should().HaveCount(2);
        personas.Should().Contain("Engineer");
        personas.Should().Contain("Architect");
    }

    [Fact]
    public async Task GetActivePersonas_NoEvents_ShouldReturnEmpty()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        var personas = await sut.GetActivePersonasAsync(
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        personas.Should().BeEmpty();
    }

    // ── GetModuleSummaryAsync ──

    [Fact]
    public async Task GetModuleSummary_WithEvents_ShouldReturnSummary()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        await SeedEventsAsync(db,
            CreateEvent(ProductModule.ServiceCatalog, userId: "user-1", persona: "Engineer"),
            CreateEvent(ProductModule.ServiceCatalog, userId: "user-2", persona: "Architect"),
            CreateEvent(ProductModule.ServiceCatalog, userId: "user-1", persona: "Engineer"));

        var summary = await sut.GetModuleSummaryAsync(
            "ServiceCatalog",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.ModuleName.Should().Be("ServiceCatalog");
        summary.TotalEvents.Should().Be(3);
        summary.UniqueUsers.Should().Be(2);
        summary.UniquePersonas.Should().Be(2);
        summary.AdoptionRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetModuleSummary_NoEvents_ShouldReturnNull()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        var summary = await sut.GetModuleSummaryAsync(
            "ServiceCatalog",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        summary.Should().BeNull();
    }

    [Fact]
    public async Task GetModuleSummary_InvalidModule_ShouldReturnNull()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        var summary = await sut.GetModuleSummaryAsync(
            "Garbage",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        summary.Should().BeNull();
    }

    [Fact]
    public async Task GetModuleSummary_EmptyModuleName_ShouldReturnNull()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        var summary = await sut.GetModuleSummaryAsync(
            "",
            FixedNow.AddHours(-1),
            FixedNow.AddHours(1),
            CancellationToken.None);

        summary.Should().BeNull();
    }

    // ── Helpers ──

    private static AnalyticsEvent CreateEvent(
        ProductModule module,
        string? userId = "user-default",
        string? persona = "Engineer") =>
        AnalyticsEvent.Create(
            tenantId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            userId: userId,
            persona: persona,
            module: module,
            eventType: AnalyticsEventType.ModuleViewed,
            feature: "list",
            entityType: null,
            outcome: null,
            route: "/test",
            teamId: null,
            domainId: null,
            sessionId: "session-test",
            clientType: "web",
            metadataJson: null,
            occurredAt: FixedNow);

    private static async Task SeedEventsAsync(
        ProductAnalyticsDbContext db,
        params AnalyticsEvent[] events)
    {
        db.AnalyticsEvents.AddRange(events);
        await db.SaveChangesAsync();
    }

    private static IProductAnalyticsModule CreateSut(ProductAnalyticsDbContext db)
        => new ProductAnalyticsModuleService(db);

    private static ProductAnalyticsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductAnalyticsDbContext>()
            .UseInMemoryDatabase($"product-analytics-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new ProductAnalyticsDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

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
        public string Id => "pa-tests-user";
        public string Name => "ProductAnalytics Tests";
        public string Email => "pa.tests@nextraceone.local";
        public string? Persona => null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
