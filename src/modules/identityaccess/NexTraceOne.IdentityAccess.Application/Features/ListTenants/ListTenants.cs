using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Pagination;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Features.ListTenants;

/// <summary>
/// Feature: ListTenants — lista todos os tenants da plataforma (uso exclusivo de Platform Admin).
///
/// Permite ao administrador da plataforma visualizar, pesquisar e gerir
/// todos os tenants registados no sistema, independentemente do tenant ativo na sessão.
/// </summary>
public static class ListTenants
{
    /// <summary>Query paginada com filtro de pesquisa opcional.</summary>
    public sealed record Query(string? Search, bool? IsActive, int Page = 1, int PageSize = 20)
        : IQuery<PagedList<TenantSummary>>, IPagedQuery;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Resumo de um tenant na listagem administrativa.</summary>
    public sealed record TenantSummary(
        Guid Id,
        string Name,
        string Slug,
        bool IsActive,
        string TenantType,
        string? LegalName,
        string? TaxId,
        Guid? ParentTenantId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    /// <summary>Handler que lista todos os tenants com paginação.</summary>
    public sealed class Handler(
        ITenantRepository tenantRepository) : IQueryHandler<Query, PagedList<TenantSummary>>
    {
        public async Task<Result<PagedList<TenantSummary>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (tenants, totalCount) = await tenantRepository.ListAsync(
                request.Search,
                request.IsActive,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = tenants.Select(t => new TenantSummary(
                t.Id.Value,
                t.Name,
                t.Slug,
                t.IsActive,
                t.TenantType.ToString(),
                t.LegalName,
                t.TaxId,
                t.ParentTenantId?.Value,
                t.CreatedAt,
                t.UpdatedAt)).ToList();

            return PagedList<TenantSummary>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }
}
