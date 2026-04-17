using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetPlatformAlertRules;

/// <summary>
/// Feature: GetPlatformAlertRules — regras de alerta da plataforma com thresholds configuráveis.
/// Retorna regras padrão sensatas com suporte a atualização de thresholds.
/// </summary>
public static class GetPlatformAlertRules
{
    /// <summary>Query sem parâmetros — retorna regras de alerta e alertas recentes.</summary>
    public sealed record Query() : IQuery<PlatformAlertsResponse>;

    /// <summary>Comando para atualizar uma regra de alerta.</summary>
    public sealed record UpdatePlatformAlertRule(
        string RuleId,
        double WarningThreshold,
        double CriticalThreshold,
        bool Enabled,
        int CooldownMinutes) : ICommand<PlatformAlertsResponse>;

    private static readonly List<PlatformAlertRuleDto> DefaultRules =
    [
        new("HighCpuUsage", "High CPU Usage", "cpu.usage.pct", 70.0, 90.0, true, 5),
        new("HighMemoryUsage", "High Memory Usage", "memory.usage.pct", 75.0, 95.0, true, 5),
        new("HighErrorRate", "High Error Rate", "http.error_rate.pct", 1.0, 5.0, true, 2),
        new("LowDiskSpace", "Low Disk Space", "disk.free.pct", 20.0, 10.0, true, 10),
        new("LongJobDuration", "Long Job Duration", "job.duration.seconds", 300.0, 600.0, true, 15)
    ];

    /// <summary>Handler de leitura das regras de alerta.</summary>
    public sealed class Handler : IQueryHandler<Query, PlatformAlertsResponse>
    {
        public Task<Result<PlatformAlertsResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new PlatformAlertsResponse(
                Rules: DefaultRules,
                RecentAlerts: [],
                ActiveAlertCount: 0);

            return Task.FromResult(Result<PlatformAlertsResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização de regra de alerta.</summary>
    public sealed class UpdateHandler : ICommandHandler<UpdatePlatformAlertRule, PlatformAlertsResponse>
    {
        public Task<Result<PlatformAlertsResponse>> Handle(UpdatePlatformAlertRule request, CancellationToken cancellationToken)
        {
            var updatedRules = DefaultRules
                .Select(r => r.RuleId == request.RuleId
                    ? r with
                    {
                        WarningThreshold = request.WarningThreshold,
                        CriticalThreshold = request.CriticalThreshold,
                        Enabled = request.Enabled,
                        CooldownMinutes = request.CooldownMinutes
                    }
                    : r)
                .ToList();

            var response = new PlatformAlertsResponse(
                Rules: updatedRules,
                RecentAlerts: [],
                ActiveAlertCount: 0);

            return Task.FromResult(Result<PlatformAlertsResponse>.Success(response));
        }
    }

    /// <summary>Resposta com regras de alerta e alertas ativos.</summary>
    public sealed record PlatformAlertsResponse(
        IReadOnlyList<PlatformAlertRuleDto> Rules,
        IReadOnlyList<PlatformAlertEventDto> RecentAlerts,
        int ActiveAlertCount);

    /// <summary>Regra de alerta da plataforma.</summary>
    public sealed record PlatformAlertRuleDto(
        string RuleId,
        string Name,
        string Metric,
        double WarningThreshold,
        double CriticalThreshold,
        bool Enabled,
        int CooldownMinutes);

    /// <summary>Evento de alerta recente.</summary>
    public sealed record PlatformAlertEventDto(
        string RuleId,
        string Severity,
        double Value,
        DateTimeOffset FiredAt);
}
