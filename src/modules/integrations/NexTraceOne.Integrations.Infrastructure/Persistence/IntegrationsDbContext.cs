using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Integrations.
/// Contém apenas a entidade IntegrationConnector, extraída de GovernanceDbContext em P2.1.
/// IngestionSource e IngestionExecution serão migradas para cá em P2.2.
///
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class IntegrationsDbContext(
    DbContextOptions<IntegrationsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Connectors de integração com sistemas externos.</summary>
    public DbSet<IntegrationConnector> IntegrationConnectors => Set<IntegrationConnector>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(IntegrationsDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "int_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
