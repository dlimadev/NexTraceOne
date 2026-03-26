using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Integrations.
/// Contém as entidades de Integrations extraídas de GovernanceDbContext em P2.1 e P2.2:
///   - IntegrationConnector (extraído em P2.1)
///   - IngestionSource (extraído em P2.2)
///   - IngestionExecution (extraído em P2.2)
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
    /// <summary>Connectors de integração com sistemas externos (extraído em P2.1).</summary>
    public DbSet<IntegrationConnector> IntegrationConnectors => Set<IntegrationConnector>();

    /// <summary>Fontes de ingestão de dados (extraído em P2.2).</summary>
    public DbSet<IngestionSource> IngestionSources => Set<IngestionSource>();

    /// <summary>Execuções de ingestão de dados (extraído em P2.2).</summary>
    public DbSet<IngestionExecution> IngestionExecutions => Set<IngestionExecution>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(IntegrationsDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "int_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
