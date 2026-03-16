using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;

/// <summary>
/// Feature: GetRunbookDetail — retorna os detalhes completos de um runbook operacional,
/// incluindo passos, pré-condições, orientação pós-validação e metadados.
/// </summary>
public static class GetRunbookDetail
{
    /// <summary>Query para obter o detalhe de um runbook.</summary>
    public sealed record Query(string RunbookId) : IQuery<Response>;

    /// <summary>Valida o identificador do runbook.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.RunbookId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe o detalhe do runbook.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = store.GetRunbookDetail(request.RunbookId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.RunbookNotFound(request.RunbookId));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com os detalhes completos do runbook.</summary>
    public sealed record Response(
        Guid RunbookId,
        string Title,
        string Summary,
        string? LinkedServiceId,
        string? LinkedIncidentType,
        IReadOnlyList<RunbookStepDto> Steps,
        IReadOnlyList<string> Preconditions,
        string? PostValidationGuidance,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    /// <summary>Passo individual do runbook.</summary>
    public sealed record RunbookStepDto(
        int StepOrder,
        string Title,
        string? Description,
        bool IsOptional);
}
