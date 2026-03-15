using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de drafts de contrato do Contract Studio.
/// Implementa consultas com filtros por status, serviço e autor, com paginação.
/// Inclui Include de Examples na consulta por Id para uso em detalhes de draft.
/// </summary>
internal sealed class ContractDraftRepository(ContractsDbContext context)
    : RepositoryBase<ContractDraft, ContractDraftId>(context), IContractDraftRepository
{
    /// <summary>Busca um draft pelo Id, incluindo a coleção de exemplos associados.</summary>
    public override async Task<ContractDraft?> GetByIdAsync(ContractDraftId id, CancellationToken ct = default)
        => await context.Drafts
            .Include(d => d.Examples)
            .SingleOrDefaultAsync(d => d.Id == id, ct);

    /// <summary>
    /// Lista drafts de contrato com filtros opcionais e paginação.
    /// Constrói a query de forma incremental conforme os filtros fornecidos.
    /// </summary>
    public async Task<IReadOnlyList<ContractDraft>> ListAsync(
        DraftStatus? status,
        Guid? serviceId,
        string? author,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(status, serviceId, author);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    /// <summary>Conta drafts de contrato que atendem aos filtros fornecidos.</summary>
    public async Task<int> CountAsync(
        DraftStatus? status,
        Guid? serviceId,
        string? author,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(status, serviceId, author);
        return await query.CountAsync(ct);
    }

    /// <summary>Lista drafts de contrato por estado.</summary>
    public async Task<IReadOnlyList<ContractDraft>> ListByStatusAsync(DraftStatus status, CancellationToken ct = default)
        => await context.Drafts
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Constrói query filtrada reutilizável para ListAsync e CountAsync.</summary>
    private IQueryable<ContractDraft> BuildFilteredQuery(
        DraftStatus? status,
        Guid? serviceId,
        string? author)
    {
        var query = context.Drafts.AsQueryable();

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (serviceId.HasValue)
            query = query.Where(d => d.ServiceId == serviceId.Value);

        if (!string.IsNullOrWhiteSpace(author))
            query = query.Where(d => d.Author == author);

        return query;
    }
}
