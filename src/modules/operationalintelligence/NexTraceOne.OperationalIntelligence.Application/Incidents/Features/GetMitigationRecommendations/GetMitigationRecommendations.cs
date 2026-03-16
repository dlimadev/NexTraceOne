using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;

/// <summary>
/// Feature: GetMitigationRecommendations — retorna recomendações de mitigação para um incidente,
/// incluindo tipo de ação sugerida, nível de risco, evidências e passos de validação.
/// </summary>
public static class GetMitigationRecommendations
{
    /// <summary>Query para obter recomendações de mitigação de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe as recomendações de mitigação do incidente.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var response = store.GetMitigationRecommendations(request.IncidentId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com recomendações de mitigação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        IReadOnlyList<MitigationRecommendationDto> Recommendations);

    /// <summary>Recomendação individual de mitigação.</summary>
    public sealed record MitigationRecommendationDto(
        Guid RecommendationId,
        string Title,
        string Summary,
        MitigationActionType RecommendedActionType,
        string RationaleSummary,
        string? EvidenceSummary,
        bool RequiresApproval,
        RiskLevel RiskLevel,
        IReadOnlyList<Guid> LinkedRunbookIds,
        IReadOnlyList<string> SuggestedValidationSteps);
}
