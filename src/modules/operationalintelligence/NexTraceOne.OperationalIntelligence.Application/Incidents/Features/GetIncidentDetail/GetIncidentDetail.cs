using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;

/// <summary>
/// Feature: GetIncidentDetail — retorna o detalhe consolidado de um incidente.
/// Inclui identidade, serviço, owner, severidade, status, timeline, correlação,
/// evidência, mudanças relacionadas, contratos, runbooks e mitigação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetIncidentDetail
{
    /// <summary>Query para obter o detalhe de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe o detalhe consolidado de um incidente.
    /// Delega ao IIncidentStore para obter os dados.
    /// </summary>
    public sealed class Handler(
        IIncidentStore store,
        IIncidentCorrelationService correlationService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Recalcula correlação com dados de changes sempre que viável,
            // evitando depender de correlação seedada estática.
            await correlationService.RecomputeAsync(request.IncidentId, cancellationToken);

            var detail = store.GetIncidentDetail(request.IncidentId);
            if (detail is null)
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            return Result<Response>.Success(detail);
        }
    }

    // ── Response records ────────────────────────────────────────────────

    /// <summary>Resposta consolidada do detalhe do incidente.</summary>
    public sealed record Response(
        IncidentIdentity Identity,
        IReadOnlyList<LinkedServiceItem> LinkedServices,
        string OwnerTeam,
        string ImpactedDomain,
        string ImpactedEnvironment,
        IReadOnlyList<TimelineEntry> Timeline,
        CorrelationSummary Correlation,
        EvidenceSummary Evidence,
        IReadOnlyList<RelatedContractItem> RelatedContracts,
        IReadOnlyList<RunbookItem> Runbooks,
        MitigationSummary Mitigation);

    /// <summary>Identidade do incidente.</summary>
    public sealed record IncidentIdentity(
        Guid IncidentId,
        string Reference,
        string Title,
        string Summary,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        IncidentStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    /// <summary>Serviço vinculado ao incidente.</summary>
    public sealed record LinkedServiceItem(
        string ServiceId, string DisplayName, string ServiceType, string Criticality);

    /// <summary>Entrada na timeline do incidente.</summary>
    public sealed record TimelineEntry(DateTimeOffset Timestamp, string Description);

    /// <summary>Resumo de correlação do incidente com mudanças e serviços.</summary>
    public sealed record CorrelationSummary(
        CorrelationConfidence Confidence,
        string Reason,
        IReadOnlyList<RelatedChangeItem> RelatedChanges,
        IReadOnlyList<RelatedServiceItem> RelatedServices);

    /// <summary>Mudança relacionada ao incidente.</summary>
    public sealed record RelatedChangeItem(
        Guid ChangeId, string Description, string ChangeType,
        string ConfidenceStatus, DateTimeOffset DeployedAt);

    /// <summary>Serviço impactado pela correlação.</summary>
    public sealed record RelatedServiceItem(
        string ServiceId, string DisplayName, string ImpactDescription);

    /// <summary>Resumo de evidências do incidente.</summary>
    public sealed record EvidenceSummary(
        string OperationalSignalsSummary,
        string DegradationSummary,
        IReadOnlyList<EvidenceItem> Observations);

    /// <summary>Evidência individual do incidente.</summary>
    public sealed record EvidenceItem(string Title, string Description);

    /// <summary>Contrato relacionado ao incidente.</summary>
    public sealed record RelatedContractItem(
        Guid ContractVersionId, string Name, string Version,
        string Protocol, string LifecycleState);

    /// <summary>Runbook vinculado ao incidente.</summary>
    public sealed record RunbookItem(string Title, string? Url);

    /// <summary>Resumo de mitigação do incidente.</summary>
    public sealed record MitigationSummary(
        MitigationStatus Status,
        IReadOnlyList<MitigationActionItem> Actions,
        string? RollbackGuidance,
        bool RollbackRelevant,
        string? EscalationGuidance);

    /// <summary>Ação de mitigação individual.</summary>
    public sealed record MitigationActionItem(
        string Description, string Status, bool Completed);
}
