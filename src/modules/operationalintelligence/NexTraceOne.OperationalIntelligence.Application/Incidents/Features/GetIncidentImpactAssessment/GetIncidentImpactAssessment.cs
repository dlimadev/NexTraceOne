using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentImpactAssessment;

/// <summary>
/// Feature: GetIncidentImpactAssessment — avalia o impacto de um incidente em termos de
/// serviços afetados, contratos impactados e amplitude operacional.
///
/// Responde às questões críticas de triagem:
///   - Quantos serviços estão afetados?
///   - Quais contratos (APIs, eventos) podem estar impactados?
///   - Qual o ambiente afetado?
///   - Qual a equipa responsável e o domínio de negócio?
///   - Qual o nível de risco de propagação?
///
/// Valor: dá ao Tech Lead e Executive visibilidade imediata do raio de impacto.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetIncidentImpactAssessment
{
    /// <summary>Query para avaliar o impacto de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe a avaliação de impacto a partir dos dados do incidente.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var detail = store.GetIncidentDetail(request.IncidentId);
            if (detail is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var correlation = store.GetIncidentCorrelation(request.IncidentId);

            var affectedServices = detail.LinkedServices
                .Select(s => new AffectedServiceItem(s.ServiceId, s.DisplayName, s.ServiceType, s.Criticality))
                .ToList();

            var correlatedServices = correlation?.RelatedServices
                .Select(s => new AffectedServiceItem(s.ServiceId, s.DisplayName, "Correlated", "Unknown"))
                .ToList() ?? new List<AffectedServiceItem>();

            var allServices = affectedServices
                .UnionBy(correlatedServices, s => s.ServiceId)
                .ToList();

            var impactedContracts = correlation?.PossibleImpactedContracts
                .Select(c => new ImpactedContractItem(c.ContractVersionId, c.Name, c.Version, c.Protocol))
                .ToList() ?? new List<ImpactedContractItem>();

            var relatedServicesCount = correlation?.RelatedServices?.Count ?? 0;
            var changeCount = correlation?.RelatedChanges?.Count ?? 0;

            var (propagationRisk, propagationRationale) = DeterminePropagationRisk(
                allServices.Count, impactedContracts.Count, relatedServicesCount);

            return Task.FromResult(Result<Response>.Success(new Response(
                IncidentId: request.IncidentId,
                Title: detail.Identity.Title,
                AffectedEnvironment: detail.ImpactedEnvironment,
                OwnerTeam: detail.OwnerTeam,
                ImpactedDomain: detail.ImpactedDomain,
                AffectedServiceCount: allServices.Count,
                AffectedServices: allServices.AsReadOnly(),
                ImpactedContractCount: impactedContracts.Count,
                ImpactedContracts: impactedContracts.AsReadOnly(),
                CorrelatedChangeCount: changeCount,
                PropagationRisk: propagationRisk,
                PropagationRationale: propagationRationale,
                ImpactSummary: BuildImpactSummary(
                    detail.Identity.Reference, detail.ImpactedEnvironment,
                    detail.OwnerTeam, detail.ImpactedDomain,
                    allServices.Count, impactedContracts.Count, changeCount))));
        }

        private static (string Risk, string Rationale) DeterminePropagationRisk(
            int serviceCount,
            int contractCount,
            int relatedServicesCount)
        {
            var hasCrossServiceCorrelation = relatedServicesCount > 1;
            var score = 0;

            if (serviceCount >= 5) score += 3;
            else if (serviceCount >= 3) score += 2;
            else if (serviceCount >= 2) score += 1;

            if (contractCount >= 3) score += 2;
            else if (contractCount >= 1) score += 1;

            if (hasCrossServiceCorrelation) score += 1;

            return score switch
            {
                >= 5 => ("Critical", "Widespread impact across multiple services and contracts. Immediate escalation required."),
                >= 3 => ("High", "Multiple services and contracts affected. Potential cascading failure risk."),
                >= 2 => ("Medium", "Limited propagation. Incident is contained to primary service and immediate dependencies."),
                _ => ("Low", "Impact appears contained to primary service.")
            };
        }

        private static string BuildImpactSummary(
            string reference,
            string environment,
            string ownerTeam,
            string impactedDomain,
            int serviceCount,
            int contractCount,
            int changeCount)
        {
            return $"Incident '{reference}' affects {serviceCount} service(s) and {contractCount} contract(s) " +
                   $"in environment '{environment}'. " +
                   (changeCount > 0
                       ? $"{changeCount} correlated change(s) detected. "
                       : string.Empty) +
                   $"Owner: {ownerTeam} | Domain: {impactedDomain}.";
        }
    }

    /// <summary>Serviço afetado pelo incidente.</summary>
    public sealed record AffectedServiceItem(
        string ServiceId,
        string DisplayName,
        string ServiceType,
        string Criticality);

    /// <summary>Contrato potencialmente impactado pelo incidente.</summary>
    public sealed record ImpactedContractItem(
        Guid ContractVersionId,
        string Name,
        string Version,
        string Protocol);

    /// <summary>Resposta da avaliação de impacto do incidente.</summary>
    public sealed record Response(
        string IncidentId,
        string Title,
        string AffectedEnvironment,
        string OwnerTeam,
        string ImpactedDomain,
        int AffectedServiceCount,
        IReadOnlyList<AffectedServiceItem> AffectedServices,
        int ImpactedContractCount,
        IReadOnlyList<ImpactedContractItem> ImpactedContracts,
        int CorrelatedChangeCount,
        string PropagationRisk,
        string PropagationRationale,
        string ImpactSummary);
}
