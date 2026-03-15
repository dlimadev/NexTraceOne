using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByService;

/// <summary>
/// Feature: ListIncidentsByService — lista incidentes filtrados por serviço.
/// Visão focada para Engineer e Tech Lead que precisam ver incidentes do seu serviço.
/// Reutiliza o modelo de listagem com filtro de serviceId pré-aplicado.
/// </summary>
public static class ListIncidentsByService
{
    /// <summary>Query para listar incidentes de um serviço específico.</summary>
    public sealed record Query(
        string ServiceId,
        IncidentStatus? Status,
        int Page,
        int PageSize) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que compõe a listagem de incidentes por serviço.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = GenerateSimulatedItems(request);

            var response = new Response(
                ServiceId: request.ServiceId,
                Items: items,
                TotalCount: items.Count,
                Page: request.Page,
                PageSize: request.PageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static IReadOnlyList<ServiceIncidentItem> GenerateSimulatedItems(Query request)
        {
            var now = DateTimeOffset.UtcNow;
            var allItems = new List<ServiceIncidentItem>
            {
                new(Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"),
                    "INC-2026-0042", "Payment Gateway — elevated error rate",
                    IncidentType.ServiceDegradation, IncidentSeverity.Critical, IncidentStatus.Mitigating,
                    "svc-payment-gateway", now.AddHours(-3), true, MitigationStatus.InProgress),

                new(Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"),
                    "INC-2026-0041", "Catalog Sync — integration partner unreachable",
                    IncidentType.DependencyFailure, IncidentSeverity.Major, IncidentStatus.Investigating,
                    "svc-catalog-sync", now.AddHours(-6), false, MitigationStatus.NotStarted),

                new(Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004"),
                    "INC-2026-0039", "Order API — latency regression after deploy",
                    IncidentType.OperationalRegression, IncidentSeverity.Minor, IncidentStatus.Resolved,
                    "svc-order-api", now.AddDays(-3), true, MitigationStatus.Verified),
            };

            var filtered = allItems
                .Where(i => i.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            if (request.Status.HasValue)
                filtered = filtered.Where(i => i.Status == request.Status.Value);

            return filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        }
    }

    /// <summary>Item de incidente de um serviço.</summary>
    public sealed record ServiceIncidentItem(
        Guid IncidentId,
        string Reference,
        string Title,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        IncidentStatus Status,
        string ServiceId,
        DateTimeOffset CreatedAt,
        bool HasCorrelatedChanges,
        MitigationStatus MitigationStatus);

    /// <summary>Resposta paginada de incidentes por serviço.</summary>
    public sealed record Response(
        string ServiceId,
        IReadOnlyList<ServiceIncidentItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
