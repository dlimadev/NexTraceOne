using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;

/// <summary>
/// Feature: GetMitigationHistory — retorna o histórico de auditoria de mitigação de um incidente,
/// incluindo ações executadas, resultados, evidências e validações.
/// </summary>
public static class GetMitigationHistory
{
    /// <summary>Query para obter o histórico de mitigação de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe o histórico de mitigação do incidente.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = store.GetMitigationHistory(request.IncidentId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o histórico de auditoria de mitigação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        IReadOnlyList<MitigationAuditEntryDto> Entries);

    /// <summary>Entrada individual no histórico de auditoria de mitigação.</summary>
    public sealed record MitigationAuditEntryDto(
        Guid EntryId,
        Guid? WorkflowId,
        string Action,
        string PerformedBy,
        DateTimeOffset PerformedAt,
        string? Notes,
        MitigationOutcome? Outcome,
        string? ValidationResult,
        IReadOnlyList<string> LinkedEvidence);
}
