using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetCustomDashboard;

/// <summary>
/// Feature: GetCustomDashboard — retorna o detalhe de um dashboard customizado por ID.
/// Consulta a base de dados real via ICustomDashboardRepository.
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

    /// <summary>Handler que obtém um dashboard da base de dados.</summary>
    public sealed class Handler(ICustomDashboardRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dashboard = await repository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "CustomDashboard.NotFound",
                    "Custom dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            return Result<Response>.Success(new Response(
                DashboardId: dashboard.Id.Value,
                Name: dashboard.Name,
                Description: dashboard.Description,
                Layout: dashboard.Layout,
                Persona: dashboard.Persona,
                WidgetIds: dashboard.WidgetIds,
                WidgetCount: dashboard.WidgetIds.Count,
                CreatedAt: dashboard.CreatedAt,
                LastModifiedAt: dashboard.UpdatedAt,
                IsShared: dashboard.IsShared));
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
