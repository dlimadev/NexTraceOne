using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;
using NexTraceOne.IntegrationTests.Infrastructure;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using Xunit;

namespace NexTraceOne.IntegrationTests.CriticalFlows;

/// <summary>
/// Testes de integração para DbContexts de cobertura estendida contra PostgreSQL real.
/// Cobre DeveloperPortalDbContext, RuntimeIntelligenceDbContext, CostIntelligenceDbContext
/// e AuditDbContext — contextos antes sem cobertura de integração real.
/// </summary>
[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class ExtendedDbContextsPostgreSqlTests(PostgreSqlIntegrationFixture fixture) : IntegrationTestBase(fixture)
{
    // ── Migrations coverage ───────────────────────────────────────────────────

    [Fact]
    public async Task AuditDatabase_Should_Have_AppliedMigrations()
    {
        var auditMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.AuditConnectionString);

        auditMigrations.Should().BeGreaterThan(0,
            "porque AuditDbContext deve ter migrations aplicadas");
    }

    [Fact]
    public async Task DeveloperPortal_Should_Have_Tables_After_Migrations()
    {
        var subscriptionsExist = await Fixture.TableExistsAsync(Fixture.DeveloperPortalConnectionString, "dp_subscriptions");

        subscriptionsExist.Should().BeTrue("dp_subscriptions deve ser criada pela migration DeveloperPortal");
    }

    [Fact]
    public async Task RuntimeIntelligence_Should_Have_Tables_After_Migrations()
    {
        var snapshotsExist = await Fixture.TableExistsAsync(Fixture.RuntimeIntelligenceConnectionString, "oi_runtime_snapshots");

        snapshotsExist.Should().BeTrue("oi_runtime_snapshots deve ser criada pela migration RuntimeIntelligence");
    }

    [Fact]
    public async Task CostIntelligence_Should_Have_Tables_After_Migrations()
    {
        var costSnapshotsExist = await Fixture.TableExistsAsync(Fixture.CostIntelligenceConnectionString, "oi_cost_snapshots");

        costSnapshotsExist.Should().BeTrue("oi_cost_snapshots deve ser criada pela migration CostIntelligence");
    }

    // ── DeveloperPortalDbContext ──────────────────────────────────────────────

    [Fact]
    public async Task DeveloperPortal_Should_Persist_Subscription_And_SavedSearch()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateDeveloperPortalDbContext();

        var subscriptionResult = Subscription.Create(
            apiAssetId: Guid.NewGuid(),
            apiName: "Payments API",
            subscriberId: Guid.NewGuid(),
            subscriberEmail: "dev@nextraceone.io",
            consumerServiceName: "checkout-service",
            consumerServiceVersion: "1.0.0",
            level: SubscriptionLevel.AllChanges,
            channel: NotificationChannel.Email,
            webhookUrl: null,
            createdAt: DateTimeOffset.UtcNow);

        subscriptionResult.IsSuccess.Should().BeTrue();
        var subscription = subscriptionResult.Value;

        var savedSearch = SavedSearch.Create(
            userId: Guid.NewGuid(),
            name: "Finance APIs",
            searchQuery: "domain:finance type:rest",
            filters: """{"tags":["payment","fintech"]}""",
            createdAt: DateTimeOffset.UtcNow);

        context.Subscriptions.Add(subscription);
        context.SavedSearches.Add(savedSearch);
        await context.SaveChangesAsync();

        var persistedSubscription = await context.Subscriptions
            .AsNoTracking()
            .SingleAsync(s => s.Id == subscription.Id);

        var persistedSearch = await context.SavedSearches
            .AsNoTracking()
            .SingleAsync(s => s.Id == savedSearch.Id);

        persistedSubscription.ApiName.Should().Be("Payments API");
        persistedSubscription.SubscriberEmail.Should().Be("dev@nextraceone.io");
        persistedSubscription.ConsumerServiceName.Should().Be("checkout-service");
        persistedSubscription.IsActive.Should().BeTrue();
        persistedSubscription.Channel.Should().Be(NotificationChannel.Email);

        persistedSearch.Name.Should().Be("Finance APIs");
        persistedSearch.SearchQuery.Should().Be("domain:finance type:rest");
        persistedSearch.Filters.Should().Contain("payment");
    }

    [Fact]
    public async Task DeveloperPortal_Should_Filter_Active_Subscriptions_ByEmail()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateDeveloperPortalDbContext();

        var sub1 = Subscription.Create(
            Guid.NewGuid(), "Orders API", Guid.NewGuid(), "dev@nextraceone.io",
            "billing-service", "1.0.0", SubscriptionLevel.BreakingChangesOnly, NotificationChannel.Email,
            null, DateTimeOffset.UtcNow).Value;

        var sub2 = Subscription.Create(
            Guid.NewGuid(), "Payments API", Guid.NewGuid(), "dev@nextraceone.io",
            "checkout-service", "2.0.0", SubscriptionLevel.AllChanges, NotificationChannel.Email,
            null, DateTimeOffset.UtcNow).Value;

        var sub3 = Subscription.Create(
            Guid.NewGuid(), "Notifications API", Guid.NewGuid(), "other@nextraceone.io",
            "platform-service", "1.0.0", SubscriptionLevel.AllChanges, NotificationChannel.Email,
            null, DateTimeOffset.UtcNow).Value;

        context.Subscriptions.AddRange(sub1, sub2, sub3);
        await context.SaveChangesAsync();

        var devSubscriptions = await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.SubscriberEmail == "dev@nextraceone.io" && s.IsActive)
            .OrderBy(s => s.ApiName)
            .ToListAsync();

        devSubscriptions.Should().HaveCount(2);
        devSubscriptions[0].ApiName.Should().Be("Orders API");
        devSubscriptions[1].ApiName.Should().Be("Payments API");
    }

    // ── RuntimeIntelligenceDbContext ──────────────────────────────────────────

    [Fact]
    public async Task RuntimeIntelligence_Should_Persist_Snapshot_With_HealthClassification()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateRuntimeIntelligenceDbContext();

        var healthySnapshot = RuntimeSnapshot.Create(
            serviceName: "orders-service",
            environment: "production",
            avgLatencyMs: 45.2m,
            p99LatencyMs: 120.5m,
            errorRate: 0.002m,
            requestsPerSecond: 850.0m,
            cpuUsagePercent: 35.0m,
            memoryUsageMb: 512.0m,
            activeInstances: 3,
            capturedAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            source: "Prometheus");

        var degradedSnapshot = RuntimeSnapshot.Create(
            serviceName: "payments-service",
            environment: "production",
            avgLatencyMs: 1200.0m,
            p99LatencyMs: 3500.0m,
            errorRate: 0.07m,
            requestsPerSecond: 200.0m,
            cpuUsagePercent: 92.0m,
            memoryUsageMb: 2048.0m,
            activeInstances: 2,
            capturedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            source: "Prometheus");

        context.RuntimeSnapshots.AddRange(healthySnapshot, degradedSnapshot);
        await context.SaveChangesAsync();

        var snapshots = await context.RuntimeSnapshots
            .AsNoTracking()
            .Where(s => s.Environment == "production")
            .OrderBy(s => s.ServiceName)
            .ToListAsync();

        snapshots.Should().HaveCount(2);

        snapshots[0].ServiceName.Should().Be("orders-service");
        snapshots[0].IsHealthy.Should().BeTrue("error rate 0.002 < 0.05 threshold");
        snapshots[0].AvgLatencyMs.Should().Be(45.2m);
        snapshots[0].P99LatencyMs.Should().Be(120.5m);

        snapshots[1].ServiceName.Should().Be("payments-service");
        snapshots[1].IsHealthy.Should().BeFalse("error rate 0.07 > 0.05 threshold");
        snapshots[1].HealthStatus.Should().NotBe(HealthStatus.Healthy);
        snapshots[1].CpuUsagePercent.Should().Be(92.0m);
    }

    [Fact]
    public async Task RuntimeIntelligence_Should_Query_Degraded_Services()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateRuntimeIntelligenceDbContext();

        var healthy = RuntimeSnapshot.Create(
            "catalog-service", "production", 32.0m, 85.0m, 0.001m, 1200.0m, 20.0m, 256.0m, 4,
            DateTimeOffset.UtcNow, "Prometheus");

        var degraded = RuntimeSnapshot.Create(
            "auth-service", "production", 800.0m, 2000.0m, 0.06m, 150.0m, 85.0m, 1024.0m, 2,
            DateTimeOffset.UtcNow, "Prometheus");

        context.RuntimeSnapshots.AddRange(healthy, degraded);
        await context.SaveChangesAsync();

        var unhealthyServices = await context.RuntimeSnapshots
            .AsNoTracking()
            .Where(s => s.HealthStatus != HealthStatus.Healthy && s.Environment == "production")
            .ToListAsync();

        unhealthyServices.Should().ContainSingle();
        unhealthyServices[0].ServiceName.Should().Be("auth-service");
    }

    // ── CostIntelligenceDbContext ─────────────────────────────────────────────

    [Fact]
    public async Task CostIntelligence_Should_Persist_CostSnapshot_And_Validate_Shares()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateCostIntelligenceDbContext();

        var snapshotResult = CostSnapshot.Create(
            serviceName: "orders-service",
            environment: "production",
            totalCost: 1250.00m,
            cpuCostShare: 600.00m,
            memoryCostShare: 350.00m,
            networkCostShare: 200.00m,
            storageCostShare: 100.00m,
            capturedAt: DateTimeOffset.UtcNow,
            source: "CloudWatch",
            period: "daily",
            currency: "USD");

        snapshotResult.IsSuccess.Should().BeTrue("a soma das parcelas não excede o custo total");
        var snapshot = snapshotResult.Value;

        context.CostSnapshots.Add(snapshot);
        await context.SaveChangesAsync();

        var persisted = await context.CostSnapshots
            .AsNoTracking()
            .SingleAsync(s => s.Id == snapshot.Id);

        persisted.ServiceName.Should().Be("orders-service");
        persisted.TotalCost.Should().Be(1250.00m);
        persisted.CpuCostShare.Should().Be(600.00m);
        persisted.Currency.Should().Be("USD");
        persisted.Period.Should().Be("daily");
        persisted.SharesSum.Should().Be(1250.00m, "cpu+mem+net+storage = total");
        persisted.UnattributedCost.Should().Be(0.00m, "todo custo foi atribuído");
    }

    [Fact]
    public async Task CostIntelligence_Should_Detect_Cost_Anomaly()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateCostIntelligenceDbContext();

        var normalSnapshotResult = CostSnapshot.Create(
            "payments-service", "production",
            500.00m, 200.00m, 150.00m, 100.00m, 50.00m,
            DateTimeOffset.UtcNow.AddDays(-1), "CloudWatch", "daily");

        var spikeSnapshotResult = CostSnapshot.Create(
            "payments-service", "production",
            950.00m, 400.00m, 300.00m, 200.00m, 50.00m,
            DateTimeOffset.UtcNow, "CloudWatch", "daily");

        normalSnapshotResult.IsSuccess.Should().BeTrue();
        spikeSnapshotResult.IsSuccess.Should().BeTrue();

        context.CostSnapshots.AddRange(normalSnapshotResult.Value, spikeSnapshotResult.Value);
        await context.SaveChangesAsync();

        var latestSnapshot = await context.CostSnapshots
            .AsNoTracking()
            .Where(s => s.ServiceName == "payments-service")
            .OrderByDescending(s => s.CapturedAt)
            .FirstAsync();

        var isAnomaly = latestSnapshot.IsAnomaly(expectedCost: 500.00m, thresholdPercent: 20);
        isAnomaly.Should().BeTrue("custo de 950 excede 500 + 20% = 600");
    }

    // ── AuditDbContext ────────────────────────────────────────────────────────

    [Fact]
    public async Task Audit_Should_Persist_AuditEvent_WithPayload()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateAuditDbContext();

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var auditEvent = AuditEvent.Record(
            sourceModule: "ChangeGovernance",
            actionType: "ReleasePromoted",
            resourceId: Guid.NewGuid().ToString(),
            resourceType: "Release",
            performedBy: "eng.lead@nextraceone.io",
            occurredAt: DateTimeOffset.UtcNow,
            tenantId: tenantId,
            payload: """{"releaseId":"abc","fromEnv":"Staging","toEnv":"Production"}""");

        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync();

        var persistedEvent = await context.AuditEvents
            .AsNoTracking()
            .SingleAsync(e => e.Id == auditEvent.Id);

        persistedEvent.SourceModule.Should().Be("ChangeGovernance");
        persistedEvent.ActionType.Should().Be("ReleasePromoted");
        persistedEvent.ResourceType.Should().Be("Release");
        persistedEvent.PerformedBy.Should().Be("eng.lead@nextraceone.io");
        persistedEvent.TenantId.Should().Be(tenantId);
        persistedEvent.Payload.Should().Contain("ReleasePromoted".Length > 0 ? "Staging" : "");
    }

    [Fact]
    public async Task Audit_Should_Support_MultipleEvents_And_Filter_ByModule()
    {
        await ResetStateAsync();

        await using var context = Fixture.CreateAuditDbContext();

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var changeEvent = AuditEvent.Record(
            "ChangeGovernance", "ReleaseCreated", Guid.NewGuid().ToString(), "Release",
            "user1@test.io", DateTimeOffset.UtcNow, tenantId);

        var contractEvent = AuditEvent.Record(
            "Contracts", "ContractApproved", Guid.NewGuid().ToString(), "ContractVersion",
            "user2@test.io", DateTimeOffset.UtcNow, tenantId);

        var identityEvent = AuditEvent.Record(
            "Identity", "UserLoggedIn", Guid.NewGuid().ToString(), "User",
            "admin@test.io", DateTimeOffset.UtcNow, tenantId);

        context.AuditEvents.AddRange(changeEvent, contractEvent, identityEvent);
        await context.SaveChangesAsync();

        var changeGovernanceEvents = await context.AuditEvents
            .AsNoTracking()
            .Where(e => e.SourceModule == "ChangeGovernance" && e.TenantId == tenantId)
            .ToListAsync();

        changeGovernanceEvents.Should().HaveCount(1);
        changeGovernanceEvents[0].ActionType.Should().Be("ReleaseCreated");

        var allEvents = await context.AuditEvents.AsNoTracking().CountAsync();
        allEvents.Should().Be(3);
    }

    // ── Cross-database: OI contexts have isolated databases ───────────────────

    [Fact]
    public async Task OperationalIntelligence_ThreeContexts_Have_Migrations_And_Coexist()
    {
        await ResetStateAsync();

        var runtimeCtx = Fixture.CreateRuntimeIntelligenceDbContext();
        var costCtx = Fixture.CreateCostIntelligenceDbContext();

        await using (runtimeCtx)
        await using (costCtx)
        {
            var snapshot = RuntimeSnapshot.Create(
                "coexistence-test-service", "production",
                50.0m, 100.0m, 0.01m, 500.0m, 30.0m, 256.0m, 2,
                DateTimeOffset.UtcNow, "Prometheus");

            var costResult = CostSnapshot.Create(
                "coexistence-test-service", "production",
                200.0m, 80.0m, 60.0m, 40.0m, 20.0m,
                DateTimeOffset.UtcNow, "CloudWatch", "daily");

            runtimeCtx.RuntimeSnapshots.Add(snapshot);
            costCtx.CostSnapshots.Add(costResult.Value);

            await runtimeCtx.SaveChangesAsync();
            await costCtx.SaveChangesAsync();
        }

        // Each context has its own isolated database — verify each has migrations applied
        var incidentMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.IncidentsConnectionString);
        var runtimeMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.RuntimeIntelligenceConnectionString);
        var costMigrations = await Fixture.GetAppliedMigrationsCountAsync(Fixture.CostIntelligenceConnectionString);

        incidentMigrations.Should().BeGreaterThan(0,
            "IncidentDbContext deve ter migrations aplicadas na sua base isolada");
        runtimeMigrations.Should().BeGreaterThan(0,
            "RuntimeIntelligenceDbContext deve ter migrations aplicadas na sua base isolada");
        costMigrations.Should().BeGreaterThan(0,
            "CostIntelligenceDbContext deve ter migrations aplicadas na sua base isolada");
    }
}
