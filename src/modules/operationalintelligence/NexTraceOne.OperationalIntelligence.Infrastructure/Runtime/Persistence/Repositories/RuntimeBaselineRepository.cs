using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;
using NexTraceOne.RuntimeIntelligence.Domain.Entities;

namespace NexTraceOne.RuntimeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de baselines de runtime para detecção de drift.
/// Implementa consulta por serviço+ambiente para unicidade de baseline.
/// </summary>
internal sealed class RuntimeBaselineRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<RuntimeBaseline, RuntimeBaselineId>(context), IRuntimeBaselineRepository
{
    /// <summary>Busca uma baseline de runtime pelo seu identificador.</summary>
    public override async Task<RuntimeBaseline?> GetByIdAsync(RuntimeBaselineId id, CancellationToken ct = default)
        => await context.RuntimeBaselines
            .SingleOrDefaultAsync(b => b.Id == id, ct);

    /// <summary>Busca a baseline de runtime de um serviço e ambiente específicos.</summary>
    public async Task<RuntimeBaseline?> GetByServiceAndEnvironmentAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
        => await context.RuntimeBaselines
            .SingleOrDefaultAsync(b => b.ServiceName == serviceName && b.Environment == environment, cancellationToken);
}
