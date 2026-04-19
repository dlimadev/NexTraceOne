using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Product Analytics.
/// Contém AnalyticsEvent extraído de GovernanceDbContext em P2.3.
///
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ProductAnalyticsDbContext(
    DbContextOptions<ProductAnalyticsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Eventos de product analytics (extraído de GovernanceDbContext em P2.3).</summary>
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    /// <summary>Definições de jornadas configuráveis (globais e por tenant).</summary>
    public DbSet<JourneyDefinition> JourneyDefinitions => Set<JourneyDefinition>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ProductAnalyticsDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "pan_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
