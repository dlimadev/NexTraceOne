using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetCustomDashboard;

/// <summary>
/// Feature: GetCustomDashboard — retorna o detalhe de um dashboard customizado por ID.
/// Nesta etapa, retorna uma estrutura de demonstração para validar o contrato da API.
///
/// Owner: módulo Governance.
/// Pilar: Governance — Source of Truth para dashboards de governance por persona.
/// </summary>
public static class GetCustomDashboard
{
    /// <summary>Query para obter um dashboard customizado específico.</summary>
    public sealed record Query(
        Guid DashboardId,
        string TenantId) : IQuery<Response>;

    /// <summary>Validação da query de obtenção de dashboard.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna a estrutura de demonstração de um dashboard.</summary>
    public sealed class Handler(IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        private static readonly IReadOnlyList<string> DemoWidgetIds =
            ["dora-metrics", "incident-summary", "service-scorecard", "cost-trend", "change-confidence", "reliability-slo"];

        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            return Task.FromResult(Result<Response>.Success(new Response(
                DashboardId: request.DashboardId,
                Name: "Team Health Overview",
                Description: "Comprehensive view of team health, reliability and change confidence.",
                Layout: "two-column",
                Persona: "TechLead",
                WidgetIds: DemoWidgetIds,
                WidgetCount: DemoWidgetIds.Count,
                CreatedAt: now.AddDays(-30),
                LastModifiedAt: now.AddDays(-2),
                IsShared: true)));
        }
    }

    /// <summary>Resposta com o detalhe completo do dashboard customizado.</summary>
    public sealed record Response(
        Guid DashboardId,
        string Name,
        string? Description,
        string Layout,
        string Persona,
        IReadOnlyList<string> WidgetIds,
        int WidgetCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastModifiedAt,
        bool IsShared);
}
