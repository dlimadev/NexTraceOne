using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence;

public sealed class TelemetryStoreDbContext(
    DbContextOptions<TelemetryStoreDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock)
{
    public DbSet<ServiceMetricsSnapshot> ServiceMetricsSnapshots => Set<ServiceMetricsSnapshot>();
    public DbSet<DependencyMetricsSnapshot> DependencyMetricsSnapshots => Set<DependencyMetricsSnapshot>();
    public DbSet<ObservedTopologyEntry> ObservedTopologyEntries => Set<ObservedTopologyEntry>();
    public DbSet<AnomalySnapshot> AnomalySnapshots => Set<AnomalySnapshot>();
    public DbSet<TelemetryReference> TelemetryReferences => Set<TelemetryReference>();
    public DbSet<ReleaseRuntimeCorrelation> ReleaseRuntimeCorrelations => Set<ReleaseRuntimeCorrelation>();
    public DbSet<InvestigationContext> InvestigationContexts => Set<InvestigationContext>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(TelemetryStoreDbContext).Assembly;

    protected override string? ConfigurationsNamespace
        => "NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Configurations";

    protected override string OutboxTableName => "ops_telstore_outbox_messages";
}
