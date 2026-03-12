using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Entities;

namespace NexTraceOne.Promotion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de ambientes de deployment, implementando consultas específicas de negócio.
/// </summary>
internal sealed class DeploymentEnvironmentRepository(PromotionDbContext context)
    : RepositoryBase<DeploymentEnvironment, DeploymentEnvironmentId>(context), IDeploymentEnvironmentRepository
{
    /// <summary>Busca um ambiente de deployment pelo identificador.</summary>
    public override async Task<DeploymentEnvironment?> GetByIdAsync(DeploymentEnvironmentId id, CancellationToken ct = default)
        => await context.DeploymentEnvironments.SingleOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>Busca um ambiente de deployment pelo nome.</summary>
    public async Task<DeploymentEnvironment?> GetByNameAsync(string name, CancellationToken ct)
        => await context.DeploymentEnvironments
            .SingleOrDefaultAsync(e => e.Name == name, ct);

    /// <summary>Lista todos os ambientes de deployment ativos ordenados por ordem sequencial.</summary>
    public async Task<IReadOnlyList<DeploymentEnvironment>> ListActiveAsync(CancellationToken ct)
        => await context.DeploymentEnvironments
            .Where(e => e.IsActive)
            .OrderBy(e => e.Order)
            .ToListAsync(ct);
}
