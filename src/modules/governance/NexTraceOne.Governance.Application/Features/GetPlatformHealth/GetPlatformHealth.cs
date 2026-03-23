using System.Diagnostics;
using System.Reflection;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPlatformHealth;

/// <summary>
/// Feature: GetPlatformHealth — saúde agregada da plataforma com estado por subsistema.
/// Consulta health checks reais via IPlatformHealthProvider para obter estado verdadeiro
/// de cada subsistema. Não utiliza valores hardcoded — subsistemas sem fonte real
/// são reportados como Unknown.
/// </summary>
public static class GetPlatformHealth
{
    /// <summary>Query sem parâmetros — retorna estado atual de todos os subsistemas.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que agrega estado de saúde real de cada subsistema da plataforma.</summary>
    public sealed class Handler(IPlatformHealthProvider healthProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var subsystemInfos = await healthProvider.GetSubsystemHealthAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            var subsystems = subsystemInfos
                .Select(info => new SubsystemHealthDto(info.Name, info.Status, info.Description, now))
                .ToList();

            var overallStatus = ComputeOverallStatus(subsystems);

            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0-preview";

            var response = new Response(
                OverallStatus: overallStatus,
                Subsystems: subsystems,
                UptimeSeconds: (long)uptime.TotalSeconds,
                Version: version,
                CheckedAt: now);

            return Result<Response>.Success(response);
        }

        private static PlatformSubsystemStatus ComputeOverallStatus(IReadOnlyList<SubsystemHealthDto> subsystems)
        {
            if (subsystems.Count == 0)
            {
                return PlatformSubsystemStatus.Unknown;
            }

            if (subsystems.Any(s => s.Status == PlatformSubsystemStatus.Unhealthy))
            {
                return PlatformSubsystemStatus.Unhealthy;
            }

            if (subsystems.Any(s => s.Status is PlatformSubsystemStatus.Degraded or PlatformSubsystemStatus.Unknown))
            {
                return PlatformSubsystemStatus.Degraded;
            }

            return PlatformSubsystemStatus.Healthy;
        }
    }

    /// <summary>Resposta de saúde da plataforma com estado geral e por subsistema.</summary>
    public sealed record Response(
        PlatformSubsystemStatus OverallStatus,
        IReadOnlyList<SubsystemHealthDto> Subsystems,
        long UptimeSeconds,
        string Version,
        DateTimeOffset CheckedAt);

    /// <summary>Estado de saúde individual de um subsistema.</summary>
    public sealed record SubsystemHealthDto(
        string Name,
        PlatformSubsystemStatus Status,
        string Description,
        DateTimeOffset LastCheckedAt);
}
