using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceReliability;

/// <summary>
/// Feature: ListServiceReliability — lista serviços com resumo de confiabilidade.
/// Retorna a visão de equipa dos serviços sob sua responsabilidade com estado atual,
/// criticidade, trend, flags operacionais e resumos de impacto.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase a composição de dados cruza módulos (Catalog, ChangeGovernance,
/// OperationalIntelligence). Os dados são simulados até integração completa entre módulos.
/// </summary>
public static class ListServiceReliability
{
    /// <summary>Query para listar serviços com resumo de confiabilidade.</summary>
    public sealed record Query(
        string? TeamId,
        string? ServiceId,
        string? Domain,
        string? Environment,
        ReliabilityStatus? Status,
        string? ServiceType,
        string? Criticality,
        string? Search,
        int Page,
        int PageSize) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.TeamId).MaximumLength(200).When(x => x.TeamId is not null);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        }
    }

    /// <summary>
    /// Handler que compõe a listagem de confiabilidade de serviços.
    /// Simula composição cross-module até integração completa.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Dados simulados para demonstrar a estrutura.
            // Em produção, isto consulta Catalog (ServiceAsset), RuntimeIntelligence (snapshots),
            // ChangeGovernance (releases) e SourceOfTruth (references) via contratos de módulo.
            var items = GenerateSimulatedItems(request);

            var response = new Response(
                Items: items,
                TotalCount: items.Count,
                Page: request.Page,
                PageSize: request.PageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static IReadOnlyList<ServiceReliabilityItem> GenerateSimulatedItems(Query request)
        {
            var allItems = new List<ServiceReliabilityItem>
            {
                new("svc-order-api", "Order API", "RestApi", "Orders", "order-squad",
                    "Critical", ReliabilityStatus.Healthy, "Operating normally",
                    TrendDirection.Stable, OperationalFlag.None, 0, false),
                new("svc-payment-gateway", "Payment Gateway", "RestApi", "Payments", "payment-squad",
                    "Critical", ReliabilityStatus.Degraded, "Elevated error rate detected",
                    TrendDirection.Declining, OperationalFlag.AnomalyDetected | OperationalFlag.RecentChangeImpact, 0, true),
                new("svc-notification-worker", "Notification Worker", "BackgroundService", "Notifications", "platform-squad",
                    "High", ReliabilityStatus.Healthy, "Processing within expected parameters",
                    TrendDirection.Improving, OperationalFlag.None, 0, false),
                new("svc-inventory-consumer", "Inventory Consumer", "KafkaConsumer", "Inventory", "order-squad",
                    "High", ReliabilityStatus.NeedsAttention, "Consumer lag increasing",
                    TrendDirection.Declining, OperationalFlag.DependencyRisk, 0, false),
                new("svc-user-service", "User Service", "RestApi", "Identity", "identity-squad",
                    "Critical", ReliabilityStatus.Healthy, "All systems operational",
                    TrendDirection.Stable, OperationalFlag.None, 0, false),
                new("svc-catalog-sync", "Catalog Sync", "IntegrationComponent", "Catalog", "platform-squad",
                    "Medium", ReliabilityStatus.Unavailable, "Integration partner unreachable",
                    TrendDirection.Declining, OperationalFlag.IncidentLinked | OperationalFlag.DependencyRisk, 1, false),
                new("svc-report-scheduler", "Report Scheduler", "ScheduledProcess", "Analytics", "data-squad",
                    "Low", ReliabilityStatus.NeedsAttention, "Coverage gap detected — missing runbook",
                    TrendDirection.Stable, OperationalFlag.CoverageGap, 0, false),
                new("svc-auth-gateway", "Auth Gateway", "SharedPlatformService", "Security", "identity-squad",
                    "Critical", ReliabilityStatus.Healthy, "All authentication flows healthy",
                    TrendDirection.Stable, OperationalFlag.None, 0, false),
            };

            var filtered = allItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(i => i.TeamName.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            if (request.Status.HasValue)
                filtered = filtered.Where(i => i.ReliabilityStatus == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Domain))
                filtered = filtered.Where(i => i.Domain.Equals(request.Domain, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.ServiceType))
                filtered = filtered.Where(i => i.ServiceType.Equals(request.ServiceType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Criticality))
                filtered = filtered.Where(i => i.Criticality.Equals(request.Criticality, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Search))
                filtered = filtered.Where(i =>
                    i.ServiceName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    i.DisplayName.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

            return filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        }
    }

    /// <summary>Item resumido de confiabilidade de um serviço na listagem.</summary>
    public sealed record ServiceReliabilityItem(
        string ServiceName,
        string DisplayName,
        string ServiceType,
        string Domain,
        string TeamName,
        string Criticality,
        ReliabilityStatus ReliabilityStatus,
        string OperationalSummary,
        TrendDirection Trend,
        OperationalFlag ActiveFlags,
        int OpenIncidents,
        bool RecentChangeImpact);

    /// <summary>Resposta paginada da listagem de confiabilidade de serviços.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceReliabilityItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
