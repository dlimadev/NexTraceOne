using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetDashboardBatchQuery;

/// <summary>
/// Feature: GetDashboardBatchQuery — executa múltiplas queries de widgets num único pedido.
/// Substitui N chamadas individuais por uma única chamada agregada, reduzindo latência e overhead HTTP.
/// Queries são executadas em paralelo via Task.WhenAll; falhas individuais de widget são isoladas.
///
/// Owner: módulo Governance.
/// Pilar: Governance — Source of Truth para dashboards de governance por persona.
/// </summary>
public static class GetDashboardBatchQuery
{
    /// <summary>Item de query para um widget individual dentro do batch.</summary>
    public sealed record WidgetQueryItem(
        string WidgetId,
        string WidgetType,
        string? ServiceId,
        string? TeamId,
        string TimeRange,
        string? MetricName,
        string? OtelEnvironment);

    /// <summary>Comando para executar um batch de queries de widgets de um dashboard.</summary>
    public sealed record Command(
        Guid DashboardId,
        IReadOnlyList<WidgetQueryItem> Widgets) : ICommand<Response>;

    /// <summary>Validação do comando de batch query.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.Widgets).NotNull().Must(w => w.Count <= 25)
                .WithMessage("Cannot batch-query more than 25 widgets at once.");
        }
    }

    /// <summary>Resultado da query de um widget individual.</summary>
    public sealed record WidgetQueryResult(
        string WidgetId,
        bool Success,
        string? ErrorCode,
        object? Data);

    /// <summary>Resposta com os resultados de todos os widgets do batch.</summary>
    public sealed record Response(IReadOnlyList<WidgetQueryResult> Results);

    /// <summary>Handler que verifica o dashboard e executa as queries dos widgets em paralelo.</summary>
    internal sealed class Handler(
        ICustomDashboardRepository dashboardRepository,
        IOtelMetricRepository otelMetricRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verificar que o dashboard existe e pertence ao tenant
            var dashboard = await dashboardRepository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);
            if (dashboard is null || dashboard.TenantId != currentTenant.Id.ToString())
                return Error.NotFound("Dashboard.NotFound", "Dashboard not found.");

            // Executar todas as queries de widgets em paralelo
            var tasks = request.Widgets.Select(w => QueryWidgetAsync(w, cancellationToken));
            var results = await Task.WhenAll(tasks);

            return new Response(results);
        }

        private async Task<WidgetQueryResult> QueryWidgetAsync(
            WidgetQueryItem widget,
            CancellationToken cancellationToken)
        {
            try
            {
                var (from, to) = ResolveTimeRange(widget.TimeRange, clock.UtcNow);

                object? data = widget.WidgetType switch
                {
                    // Métricas OTel — consulta real via repositório
                    "otel-metrics" when widget.MetricName is not null =>
                        await otelMetricRepository.QueryAsync(
                            widget.ServiceId ?? string.Empty,
                            widget.MetricName,
                            from, to,
                            widget.OtelEnvironment,
                            cancellationToken),

                    // Para outros tipos de widget, retornar sumário placeholder
                    _ => new { widgetId = widget.WidgetId, type = widget.WidgetType, cached = false }
                };

                return new WidgetQueryResult(widget.WidgetId, true, null, data);
            }
            catch (Exception ex)
            {
                return new WidgetQueryResult(widget.WidgetId, false, "QueryFailed", new { error = ex.Message });
            }
        }

        private static (DateTimeOffset from, DateTimeOffset to) ResolveTimeRange(
            string timeRange, DateTimeOffset now) => timeRange switch
        {
            "1h"  => (now.AddHours(-1), now),
            "6h"  => (now.AddHours(-6), now),
            "24h" => (now.AddHours(-24), now),
            "7d"  => (now.AddDays(-7), now),
            "30d" => (now.AddDays(-30), now),
            _     => (now.AddHours(-24), now),
        };
    }
}
