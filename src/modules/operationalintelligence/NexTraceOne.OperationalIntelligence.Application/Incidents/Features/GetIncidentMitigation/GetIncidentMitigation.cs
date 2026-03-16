using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;

/// <summary>
/// Feature: GetIncidentMitigation — retorna informações de mitigação e runbooks de um incidente.
/// Inclui ações sugeridas, runbooks recomendados, status de mitigação,
/// orientação de rollback e recomendação de escalonamento.
/// </summary>
public static class GetIncidentMitigation
{
    /// <summary>Query para obter a mitigação de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe a mitigação do incidente.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var mitigation = store.GetIncidentMitigation(request.IncidentId);
            if (mitigation is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(mitigation));
        }
    }

    /// <summary>Resposta de mitigação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        MitigationStatus MitigationStatus,
        IReadOnlyList<SuggestedAction> SuggestedActions,
        IReadOnlyList<RecommendedRunbook> RecommendedRunbooks,
        string? RollbackGuidance,
        bool RollbackRelevant,
        string? EscalationGuidance);

    /// <summary>Ação sugerida de mitigação.</summary>
    public sealed record SuggestedAction(string Description, string Status, bool Completed);

    /// <summary>Runbook recomendado.</summary>
    public sealed record RecommendedRunbook(string Title, string? Url, string? Description);
}
