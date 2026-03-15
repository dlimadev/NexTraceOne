using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;

/// <summary>
/// Feature: ListRunbooks — retorna a lista de runbooks operacionais disponíveis,
/// com suporte a filtragem por serviço, tipo de incidente e pesquisa textual.
/// </summary>
public static class ListRunbooks
{
    /// <summary>Query para listar runbooks com filtros opcionais.</summary>
    public sealed record Query(string? ServiceId, string? IncidentType, string? Search) : IQuery<Response>;

    /// <summary>Valida os parâmetros opcionais da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.IncidentType).MaximumLength(200).When(x => x.IncidentType is not null);
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        }
    }

    /// <summary>Handler que retorna a lista simulada de runbooks.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private static readonly List<RunbookSummaryDto> AllRunbooks = new()
        {
            new RunbookSummaryDto(
                RunbookId: Guid.Parse("rb000001-0001-0000-0000-000000000001"),
                Title: "Payment Gateway Rollback Procedure",
                Summary: "Step-by-step guide for rolling back the payment-service deployment.",
                LinkedServiceId: "payment-service",
                LinkedIncidentType: "ServiceDegradation",
                StepCount: 6,
                CreatedAt: DateTimeOffset.Parse("2024-01-15T09:00:00Z")),
            new RunbookSummaryDto(
                RunbookId: Guid.Parse("rb000002-0001-0000-0000-000000000001"),
                Title: "Catalog Sync Manual Recovery",
                Summary: "Steps for manually recovering catalog synchronization.",
                LinkedServiceId: "catalog-service",
                LinkedIncidentType: "DependencyFailure",
                StepCount: 4,
                CreatedAt: DateTimeOffset.Parse("2024-02-10T11:00:00Z")),
            new RunbookSummaryDto(
                RunbookId: Guid.Parse("rb000003-0001-0000-0000-000000000001"),
                Title: "Generic Service Restart Procedure",
                Summary: "Standard procedure for performing a controlled restart of a service.",
                LinkedServiceId: null,
                LinkedIncidentType: null,
                StepCount: 4,
                CreatedAt: DateTimeOffset.Parse("2024-03-01T08:00:00Z")),
        };

        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var filtered = AllRunbooks.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(r =>
                    r.LinkedServiceId is not null
                    && r.LinkedServiceId.Contains(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.IncidentType))
                filtered = filtered.Where(r =>
                    r.LinkedIncidentType is not null
                    && r.LinkedIncidentType.Equals(request.IncidentType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Search))
                filtered = filtered.Where(r =>
                    r.Title.Contains(request.Search, StringComparison.OrdinalIgnoreCase)
                    || r.Summary.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

            var response = new Response(Runbooks: filtered.ToList().AsReadOnly());
            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com a lista de runbooks.</summary>
    public sealed record Response(IReadOnlyList<RunbookSummaryDto> Runbooks);

    /// <summary>Resumo de um runbook operacional.</summary>
    public sealed record RunbookSummaryDto(
        Guid RunbookId,
        string Title,
        string Summary,
        string? LinkedServiceId,
        string? LinkedIncidentType,
        int StepCount,
        DateTimeOffset CreatedAt);
}
