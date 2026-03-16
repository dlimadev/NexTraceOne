using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;

/// <summary>
/// Feature: GetMitigationValidation — retorna o estado de validação pós-mitigação de um workflow,
/// incluindo verificações esperadas, resultado observado e resumo de sinais pós-mitigação.
/// </summary>
public static class GetMitigationValidation
{
    /// <summary>Query para obter a validação de um workflow de mitigação.</summary>
    public sealed record Query(string IncidentId, string WorkflowId) : IQuery<Response>;

    /// <summary>Valida os identificadores do incidente e do workflow.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe os dados de validação do workflow de mitigação.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = store.GetMitigationValidation(request.IncidentId, request.WorkflowId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o estado de validação pós-mitigação do workflow.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus Status,
        IReadOnlyList<ValidationCheckDto> ExpectedChecks,
        string? ObservedOutcome,
        string? PostMitigationSignalsSummary,
        DateTimeOffset? ValidatedAt,
        string? ValidatedBy);

    /// <summary>Verificação individual de validação pós-mitigação.</summary>
    public sealed record ValidationCheckDto(
        string CheckName,
        string? Description,
        bool IsPassed,
        string? ObservedValue);
}
