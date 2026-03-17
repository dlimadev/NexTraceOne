using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório de perfis de maturidade de observabilidade por serviço.
/// Implementa consulta por serviço+ambiente para unicidade de perfil.
/// </summary>
internal sealed class ObservabilityProfileRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<ObservabilityProfile, ObservabilityProfileId>(context), IObservabilityProfileRepository
{
    /// <summary>Busca um perfil de observabilidade pelo seu identificador.</summary>
    public override async Task<ObservabilityProfile?> GetByIdAsync(ObservabilityProfileId id, CancellationToken ct = default)
        => await context.ObservabilityProfiles
            .SingleOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Busca o perfil de observabilidade de um serviço e ambiente específicos.</summary>
    public async Task<ObservabilityProfile?> GetByServiceAndEnvironmentAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
        => await context.ObservabilityProfiles
            .SingleOrDefaultAsync(p => p.ServiceName == serviceName && p.Environment == environment, cancellationToken);
}
