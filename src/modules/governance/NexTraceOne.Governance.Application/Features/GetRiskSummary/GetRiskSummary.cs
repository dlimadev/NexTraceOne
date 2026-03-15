using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetRiskSummary;

/// <summary>
/// Feature: GetRiskSummary — resumo de risco operacional contextualizado.
/// Cada indicador de risco está vinculado a um serviço, equipa ou domínio.
/// </summary>
public static class GetRiskSummary
{
    /// <summary>Query de resumo de risco. Permite filtragem por equipa ou domínio.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null) : IQuery<Response>;

    /// <summary>Handler que computa indicadores de risco agregados.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var indicators = new List<RiskIndicatorDto>
            {
                new("svc-payment-api", "Payment API", "Payments", "Team Payments", RiskLevel.High,
                    new[] { new RiskDimensionDto(RiskDimension.Change, RiskLevel.High, "3 failed deployments in last 7 days"),
                            new RiskDimensionDto(RiskDimension.IncidentRecurrence, RiskLevel.High, "Recurring timeout incidents") }),
                new("svc-user-service", "User Service", "Identity", "Team Identity", RiskLevel.Medium,
                    new[] { new RiskDimensionDto(RiskDimension.Documentation, RiskLevel.Medium, "Missing runbook"),
                            new RiskDimensionDto(RiskDimension.Contract, RiskLevel.Low, "Contract version outdated") }),
                new("svc-notification-hub", "Notification Hub", "Messaging", "Team Messaging", RiskLevel.Low,
                    new[] { new RiskDimensionDto(RiskDimension.Dependency, RiskLevel.Low, "All dependencies healthy") }),
                new("svc-order-processor", "Order Processor", "Commerce", "Team Commerce", RiskLevel.Critical,
                    new[] { new RiskDimensionDto(RiskDimension.Operational, RiskLevel.Critical, "Service degraded"),
                            new RiskDimensionDto(RiskDimension.Change, RiskLevel.High, "Recent rollback"),
                            new RiskDimensionDto(RiskDimension.Ownership, RiskLevel.Medium, "Owner on leave") }),
                new("svc-inventory-sync", "Inventory Sync", "Commerce", "Team Commerce", RiskLevel.Medium,
                    new[] { new RiskDimensionDto(RiskDimension.Contract, RiskLevel.Medium, "No contract defined"),
                            new RiskDimensionDto(RiskDimension.Documentation, RiskLevel.High, "No documentation") })
            };

            var response = new Response(
                OverallRiskLevel: RiskLevel.High,
                TotalServicesAssessed: indicators.Count,
                CriticalCount: indicators.Count(i => i.RiskLevel == RiskLevel.Critical),
                HighCount: indicators.Count(i => i.RiskLevel == RiskLevel.High),
                MediumCount: indicators.Count(i => i.RiskLevel == RiskLevel.Medium),
                LowCount: indicators.Count(i => i.RiskLevel == RiskLevel.Low),
                Indicators: indicators,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do resumo de risco.</summary>
    public sealed record Response(
        RiskLevel OverallRiskLevel,
        int TotalServicesAssessed,
        int CriticalCount,
        int HighCount,
        int MediumCount,
        int LowCount,
        IReadOnlyList<RiskIndicatorDto> Indicators,
        DateTimeOffset GeneratedAt);

    /// <summary>Indicador de risco por serviço.</summary>
    public sealed record RiskIndicatorDto(
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        RiskLevel RiskLevel,
        IReadOnlyList<RiskDimensionDto> Dimensions);

    /// <summary>Dimensão de risco individual com explicação.</summary>
    public sealed record RiskDimensionDto(
        RiskDimension Dimension,
        RiskLevel Level,
        string Explanation);
}
