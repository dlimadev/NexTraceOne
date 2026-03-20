using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

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
    {
        var drafts = await context.Drafts
            .IgnoreQueryFilters()
            .Include(d => d.Examples)
            .ToListAsync(ct);

        return drafts.SingleOrDefault(d => !d.IsDeleted && d.Id.Value == id.Value);
    }

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
        => await BuildFilteredQuery(status, null, null)
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
