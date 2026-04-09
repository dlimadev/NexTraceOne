using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de negociações cross-team de contratos.
/// Persiste e consulta negociações para rastreabilidade do fluxo de aprovação colaborativa.
/// </summary>
internal sealed class ContractNegotiationRepository(ContractsDbContext context)
    : IContractNegotiationRepository
{
    /// <inheritdoc />
    public async Task<ContractNegotiation?> GetByIdAsync(ContractNegotiationId id, CancellationToken cancellationToken)
        => await context.ContractNegotiations
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractNegotiation>> ListAsync(NegotiationStatus? status, Guid? teamId, CancellationToken cancellationToken)
    {
        var query = context.ContractNegotiations.AsNoTracking();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (teamId.HasValue)
            query = query.Where(x => x.ProposedByTeamId == teamId.Value);

        return await query
            .OrderByDescending(x => x.LastActivityAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ContractNegotiation negotiation, CancellationToken cancellationToken)
        => await context.ContractNegotiations.AddAsync(negotiation, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(ContractNegotiation negotiation, CancellationToken cancellationToken)
    {
        context.ContractNegotiations.Update(negotiation);
        return Task.CompletedTask;
    }
}
