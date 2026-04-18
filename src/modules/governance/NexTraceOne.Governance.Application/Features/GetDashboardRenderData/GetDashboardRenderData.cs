using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetDashboardRenderData;

/// <summary>
/// Feature: GetDashboardRenderData — retorna a estrutura completa de um dashboard para rendering.
/// Fornece ao frontend todos os metadados necessários para renderizar o layout do dashboard:
/// posições, tipos e configurações de cada widget. Cada widget busca os seus próprios dados
/// independentemente via TanStack Query no frontend.
///
/// Owner: módulo Governance.
/// Pilar: Governance — Source of Truth para dashboards de governance por persona.
/// </summary>
public static class GetDashboardRenderData
{
    /// <summary>Query para obter os dados de rendering de um dashboard.</summary>
    public sealed record Query(
        Guid DashboardId,
        string TenantId,
        string? EnvironmentId = null,
        string? GlobalTimeRange = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que prepara os dados de rendering do dashboard.</summary>
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

            if (dashboard.TenantId != request.TenantId)
                return Error.Forbidden(
                    "CustomDashboard.Forbidden",
                    "Access to dashboard '{0}' is not allowed.",
                    request.DashboardId);

            var effectiveTimeRange = request.GlobalTimeRange ?? "24h";

            var widgetSlots = dashboard.Widgets.Select(w => new WidgetSlot(
                WidgetId: w.WidgetId,
                Type: w.Type,
                PosX: w.Position.X,
                PosY: w.Position.Y,
                Width: w.Position.Width,
                Height: w.Position.Height,
                EffectiveServiceId: w.Config.ServiceId,
                EffectiveTeamId: w.Config.TeamId,
                EffectiveTimeRange: w.Config.TimeRange ?? effectiveTimeRange,
                CustomTitle: w.Config.CustomTitle)).ToList();

            return Result<Response>.Success(new Response(
                DashboardId: dashboard.Id.Value,
                Name: dashboard.Name,
                Layout: dashboard.Layout,
                Persona: dashboard.Persona,
                EnvironmentId: request.EnvironmentId,
                GlobalTimeRange: effectiveTimeRange,
                Widgets: widgetSlots,
                GeneratedAt: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Slot de widget resolvido com todos os parâmetros efectivos para rendering.</summary>
    public sealed record WidgetSlot(
        string WidgetId,
        string Type,
        int PosX,
        int PosY,
        int Width,
        int Height,
        string? EffectiveServiceId,
        string? EffectiveTeamId,
        string EffectiveTimeRange,
        string? CustomTitle);

    /// <summary>Resposta com a estrutura completa do dashboard para o frontend renderizar.</summary>
    public sealed record Response(
        Guid DashboardId,
        string Name,
        string Layout,
        string Persona,
        string? EnvironmentId,
        string GlobalTimeRange,
        IReadOnlyList<WidgetSlot> Widgets,
        DateTimeOffset GeneratedAt);
}
