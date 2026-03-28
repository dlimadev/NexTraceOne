using System.Reflection;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPlatformReadiness;

/// <summary>
/// Feature: GetPlatformReadiness — avaliação de prontidão da plataforma para receber tráfego.
/// Delega para IPlatformHealthProvider para obter o estado real de cada subsistema,
/// convertendo health checks em readiness checks (Healthy/Degraded = pronto; Unknown/Unhealthy = não pronto).
/// </summary>
public static class GetPlatformReadiness
{
    /// <summary>Query sem parâmetros — retorna avaliação de readiness de todos os subsistemas.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que avalia readiness real de cada subsistema via IPlatformHealthProvider.</summary>
    public sealed class Handler(IPlatformHealthProvider healthProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0-preview";
            var environmentName =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production";

            var subsystems = await healthProvider.GetSubsystemHealthAsync(cancellationToken);

            // Degraded is considered "ready" — system can serve traffic with caveats.
            // Unknown means no health check available; we do NOT assume ready.
            var checks = subsystems
                .Select(s => new ReadinessCheckDto(
                    s.Name,
                    s.Status is PlatformSubsystemStatus.Healthy or PlatformSubsystemStatus.Degraded,
                    s.Description))
                .ToList();

            var isReady = checks.TrueForAll(c => c.Passed);

            var response = new Response(
                IsReady: isReady,
                EnvironmentName: environmentName,
                Version: version,
                Checks: checks,
                CheckedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta de readiness da plataforma com estado agregado e por subsistema.</summary>
    public sealed record Response(
        bool IsReady,
        string EnvironmentName,
        string Version,
        IReadOnlyList<ReadinessCheckDto> Checks,
        DateTimeOffset CheckedAt);

    /// <summary>Resultado individual de um check de readiness.</summary>
    public sealed record ReadinessCheckDto(
        string Name,
        bool Passed,
        string Description);
}
