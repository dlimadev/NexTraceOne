using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;

/// <summary>
/// Feature: GetIncidentCorrelation — retorna a correlação detalhada de um incidente.
/// Inclui mudanças relacionadas, confiança da correlação, serviços impactados,
/// dependências e contratos/eventos possivelmente afetados.
/// Base para futura IA operacional.
/// </summary>
public static class GetIncidentCorrelation
{
    /// <summary>Query para obter a correlação de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe a correlação do incidente.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var correlation = store.GetIncidentCorrelation(request.IncidentId);
            if (correlation is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(correlation));
        }
    }

    /// <summary>Resposta de correlação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        CorrelationConfidence Confidence,
        string Reason,
        IReadOnlyList<CorrelatedChange> RelatedChanges,
        IReadOnlyList<CorrelatedService> RelatedServices,
        IReadOnlyList<CorrelatedDependency> RelatedDependencies,
        IReadOnlyList<ImpactedContract> PossibleImpactedContracts);

    /// <summary>Mudança correlacionada.</summary>
    public sealed record CorrelatedChange(
        Guid ChangeId, string Description, string ChangeType,
        string ConfidenceStatus, DateTimeOffset DeployedAt);

    /// <summary>Serviço correlacionado.</summary>
    public sealed record CorrelatedService(
        string ServiceId, string DisplayName, string ImpactDescription);

    /// <summary>Dependência correlacionada.</summary>
    public sealed record CorrelatedDependency(
        string ServiceId, string DisplayName, string Relationship);

    /// <summary>Contrato possivelmente impactado.</summary>
    public sealed record ImpactedContract(
        Guid ContractVersionId, string Name, string Version, string Protocol);
}
