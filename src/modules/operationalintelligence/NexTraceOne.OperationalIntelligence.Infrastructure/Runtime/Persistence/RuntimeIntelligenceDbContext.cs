using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;

/// <summary>
/// DbContext do módulo RuntimeIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Base de dados isolada por serviço — cada módulo possui sua própria connection string.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class RuntimeIntelligenceDbContext(
    DbContextOptions<RuntimeIntelligenceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Snapshots de saúde e performance de serviços em runtime.</summary>
    public DbSet<RuntimeSnapshot> RuntimeSnapshots => Set<RuntimeSnapshot>();

    /// <summary>Baselines de métricas de runtime para comparação de drift.</summary>
    public DbSet<RuntimeBaseline> RuntimeBaselines => Set<RuntimeBaseline>();

    /// <summary>Findings de drift detectados entre baselines e snapshots atuais.</summary>
    public DbSet<DriftFinding> DriftFindings => Set<DriftFinding>();

    /// <summary>Perfis de maturidade de observabilidade por serviço.</summary>
    public DbSet<ObservabilityProfile> ObservabilityProfiles => Set<ObservabilityProfile>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(RuntimeIntelligenceDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
