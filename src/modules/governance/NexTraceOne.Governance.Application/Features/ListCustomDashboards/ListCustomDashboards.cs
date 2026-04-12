using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.ListCustomDashboards;

/// <summary>
/// Feature: ListCustomDashboards — lista dashboards customizados do tenant com filtro por persona.
/// Consulta a base de dados real via ICustomDashboardRepository.
///
/// Owner: módulo Governance.
/// Pilar: Governance — Source of Truth para dashboards de governance por persona.
/// </summary>
public static class ListCustomDashboards
{
    /// <summary>Query para listar dashboards customizados com paginação e filtro por persona.</summary>
    public sealed record Query(
        string TenantId,
        string? Persona = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Validação da query de listagem de dashboards.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }

    /// <summary>Handler que consulta dashboards customizados da base de dados.</summary>
    public sealed class Handler(ICustomDashboardRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var allItems = await repository.ListAsync(request.Persona, cancellationToken);

            var paged = allItems
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => new DashboardSummary(
                    DashboardId: d.Id.Value,
                    Name: d.Name,
                    Persona: d.Persona,
                    WidgetCount: d.WidgetIds.Count,
                    Layout: d.Layout,
                    IsShared: d.IsShared,
                    CreatedAt: d.CreatedAt))
                .ToList();

            return Result<Response>.Success(new Response(
                Items: paged,
                TotalCount: allItems.Count));
        }
    }

    /// <summary>Resposta paginada com a lista de dashboards customizados.</summary>
    public sealed record Response(
        IReadOnlyList<DashboardSummary> Items,
        int TotalCount);

    /// <summary>Resumo de um dashboard customizado para listagem.</summary>
    public sealed record DashboardSummary(
        Guid DashboardId,
        string Name,
        string Persona,
        int WidgetCount,
        string Layout,
        bool IsShared,
        DateTimeOffset CreatedAt);
}
