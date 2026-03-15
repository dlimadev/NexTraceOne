using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;

namespace NexTraceOne.Contracts.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de revisões de contrato do Contract Studio.
/// Armazena o histórico de decisões de aprovação e rejeição para auditoria completa.
/// </summary>
internal sealed class ContractReviewRepository(ContractsDbContext context)
    : RepositoryBase<ContractReview, ContractReviewId>(context), IContractReviewRepository
{
    /// <summary>Lista revisões vinculadas a um draft, ordenadas por data de revisão.</summary>
    public async Task<IReadOnlyList<ContractReview>> ListByDraftAsync(ContractDraftId draftId, CancellationToken ct = default)
        => await context.Reviews
            .Where(r => r.DraftId == draftId)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync(ct);
}
