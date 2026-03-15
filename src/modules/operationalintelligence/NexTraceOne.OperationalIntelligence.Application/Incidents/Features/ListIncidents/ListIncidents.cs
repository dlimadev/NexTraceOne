using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;

/// <summary>
/// Feature: ListIncidents — lista incidentes com filtros contextualizados.
/// Retorna resumo de incidentes com correlação, severidade, status, serviço e mitigação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class ListIncidents
{
    /// <summary>Query para listar incidentes com filtros.</summary>
    public sealed record Query(
        string? TeamId,
        string? ServiceId,
        string? Environment,
        IncidentSeverity? Severity,
        IncidentStatus? Status,
        IncidentType? IncidentType,
        string? Search,
        DateTimeOffset? From,
        DateTimeOffset? To,
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
            RuleFor(x => x.Environment).MaximumLength(200).When(x => x.Environment is not null);
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        }
    }

    /// <summary>
    /// Handler que compõe a listagem de incidentes.
    /// Simula composição cross-module até integração completa.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = GenerateSimulatedItems(request);

            var response = new Response(
                Items: items,
                TotalCount: items.Count,
                Page: request.Page,
                PageSize: request.PageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static IReadOnlyList<IncidentListItem> GenerateSimulatedItems(Query request)
        {
            var now = DateTimeOffset.UtcNow;
            var allItems = new List<IncidentListItem>
            {
                new(Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"),
                    "INC-2026-0042", "Payment Gateway — elevated error rate",
                    IncidentType.ServiceDegradation, IncidentSeverity.Critical, IncidentStatus.Mitigating,
                    "svc-payment-gateway", "Payment Gateway", "payment-squad",
                    "Production", now.AddHours(-3), true, CorrelationConfidence.High, MitigationStatus.InProgress),

                new(Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"),
                    "INC-2026-0041", "Catalog Sync — integration partner unreachable",
                    IncidentType.DependencyFailure, IncidentSeverity.Major, IncidentStatus.Investigating,
                    "svc-catalog-sync", "Catalog Sync", "platform-squad",
                    "Production", now.AddHours(-6), false, CorrelationConfidence.Low, MitigationStatus.NotStarted),

                new(Guid.Parse("a1b2c3d4-0003-0000-0000-000000000003"),
                    "INC-2026-0040", "Inventory Consumer — consumer lag spike",
                    IncidentType.MessagingIssue, IncidentSeverity.Major, IncidentStatus.Monitoring,
                    "svc-inventory-consumer", "Inventory Consumer", "order-squad",
                    "Production", now.AddDays(-1), true, CorrelationConfidence.Medium, MitigationStatus.Applied),

                new(Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004"),
                    "INC-2026-0039", "Order API — latency regression after deploy",
                    IncidentType.OperationalRegression, IncidentSeverity.Minor, IncidentStatus.Resolved,
                    "svc-order-api", "Order API", "order-squad",
                    "Production", now.AddDays(-3), true, CorrelationConfidence.Confirmed, MitigationStatus.Verified),

                new(Guid.Parse("a1b2c3d4-0005-0000-0000-000000000005"),
                    "INC-2026-0038", "Notification Worker — background job failures",
                    IncidentType.BackgroundProcessingIssue, IncidentSeverity.Warning, IncidentStatus.Closed,
                    "svc-notification-worker", "Notification Worker", "platform-squad",
                    "Production", now.AddDays(-7), false, CorrelationConfidence.NotAssessed, MitigationStatus.Verified),

                new(Guid.Parse("a1b2c3d4-0006-0000-0000-000000000006"),
                    "INC-2026-0037", "Auth Gateway — contract schema mismatch",
                    IncidentType.ContractImpact, IncidentSeverity.Major, IncidentStatus.Resolved,
                    "svc-auth-gateway", "Auth Gateway", "identity-squad",
                    "Staging", now.AddDays(-5), true, CorrelationConfidence.High, MitigationStatus.Verified),
            };

            var filtered = allItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(i => i.OwnerTeam.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(i => i.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Environment))
                filtered = filtered.Where(i => i.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase));

            if (request.Severity.HasValue)
                filtered = filtered.Where(i => i.Severity == request.Severity.Value);

            if (request.Status.HasValue)
                filtered = filtered.Where(i => i.Status == request.Status.Value);

            if (request.IncidentType.HasValue)
                filtered = filtered.Where(i => i.IncidentType == request.IncidentType.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
                filtered = filtered.Where(i =>
                    i.Title.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    i.Reference.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    i.ServiceDisplayName.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

            if (request.From.HasValue)
                filtered = filtered.Where(i => i.CreatedAt >= request.From.Value);

            if (request.To.HasValue)
                filtered = filtered.Where(i => i.CreatedAt <= request.To.Value);

            return filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        }
    }

    /// <summary>Item resumido de incidente na listagem.</summary>
    public sealed record IncidentListItem(
        Guid IncidentId,
        string Reference,
        string Title,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        IncidentStatus Status,
        string ServiceId,
        string ServiceDisplayName,
        string OwnerTeam,
        string Environment,
        DateTimeOffset CreatedAt,
        bool HasCorrelatedChanges,
        CorrelationConfidence CorrelationConfidence,
        MitigationStatus MitigationStatus);

    /// <summary>Resposta paginada da listagem de incidentes.</summary>
    public sealed record Response(
        IReadOnlyList<IncidentListItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
