using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Domain.Entities;

namespace NexTraceOne.CostIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de perfis de custo de serviços com orçamento e alertas.
/// Implementa consulta por serviço+ambiente para unicidade de perfil.
/// </summary>
internal sealed class ServiceCostProfileRepository(CostIntelligenceDbContext context)
    : RepositoryBase<ServiceCostProfile, ServiceCostProfileId>(context), IServiceCostProfileRepository
{
    /// <summary>Busca um perfil de custo pelo seu identificador.</summary>
    public override async Task<ServiceCostProfile?> GetByIdAsync(ServiceCostProfileId id, CancellationToken ct = default)
        => await context.ServiceCostProfiles
            .SingleOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Busca o perfil de custo de um serviço e ambiente específicos.</summary>
    public async Task<ServiceCostProfile?> GetByServiceAndEnvironmentAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
        => await context.ServiceCostProfiles
            .SingleOrDefaultAsync(p => p.ServiceName == serviceName && p.Environment == environment, cancellationToken);
}
