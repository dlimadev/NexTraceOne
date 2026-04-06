using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ListCustomDashboards;

/// <summary>
/// Feature: ListCustomDashboards — lista dashboards customizados do tenant com filtro por persona.
/// Nesta etapa, retorna dados de demonstração para validar o contrato e a navegação.
///
/// Owner: módulo Governance.
/// Pilar: Governance — Builder visual para personas criarem dashboards customizados.
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

    /// <summary>Handler que retorna uma lista de demonstração de dashboards customizados.</summary>
    public sealed class Handler(IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var allItems = new List<DashboardSummary>
            {
                new(
                    DashboardId: new Guid("11111111-0000-0000-0000-000000000001"),
                    Name: "Executive KPI Overview",
                    Persona: "Executive",
                    WidgetCount: 6,
                    Layout: "grid",
                    IsShared: true,
                    CreatedAt: now.AddDays(-60)),
                new(
                    DashboardId: new Guid("11111111-0000-0000-0000-000000000002"),
                    Name: "Team Health Dashboard",
                    Persona: "TechLead",
                    WidgetCount: 5,
                    Layout: "two-column",
                    IsShared: false,
                    CreatedAt: now.AddDays(-30)),
                new(
                    DashboardId: new Guid("11111111-0000-0000-0000-000000000003"),
                    Name: "Engineer Daily View",
                    Persona: "Engineer",
                    WidgetCount: 4,
                    Layout: "single-column",
                    IsShared: false,
                    CreatedAt: now.AddDays(-7)),
            };

            var filtered = string.IsNullOrWhiteSpace(request.Persona)
                ? allItems
                : allItems.Where(d => d.Persona.Equals(request.Persona, StringComparison.OrdinalIgnoreCase)).ToList();

            return Task.FromResult(Result<Response>.Success(new Response(
                Items: filtered,
                TotalCount: filtered.Count)));
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
