using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Contracts.Contracts.ServiceInterfaces;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;
using NexTraceOne.Knowledge.Contracts;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;

namespace NexTraceOne.Catalog.Application.SourceOfTruth.Features.ComputeServiceScorecard;

/// <summary>
/// Feature: ComputeServiceScorecard — calcula um scorecard de maturidade
/// para um serviço consultando dados de múltiplos módulos cross-module.
///
/// 8 dimensões: Ownership (10%), Documentação (10%), Contratos (15%), SLOs (15%),
/// Observabilidade (15%), Change Governance (15%), Runbooks (10%), Segurança (10%).
///
/// Diferencial NexTraceOne: scorecard que inclui cobertura de contratos + change
/// governance + SLOs reais — visão unificada que nenhum concorrente oferece.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeServiceScorecard
{
    /// <summary>Query para calcular scorecard de maturidade de um serviço.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment = "Production") : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Environment)
                .NotEmpty()
                .MaximumLength(50);
        }
    }

    /// <summary>Handler que calcula o scorecard cross-module de maturidade do serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceRepository,
        IApiAssetRepository apiRepository,
        ILinkedReferenceRepository referenceRepository,
        IContractsModule contractsModule,
        IReliabilityModule reliabilityModule,
        IKnowledgeModule knowledgeModule) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // ── Buscar serviço pelo nome ─────────────────────────────
            var service = await serviceRepository.GetByNameAsync(request.ServiceName, cancellationToken);

            if (service is null)
                return Error.NotFound(
                    "ServiceScorecard.ServiceNotFound",
                    "Service '{0}' not found",
                    request.ServiceName);

            var serviceId = service.Id.Value;

            // ── Buscar dados complementares ──────────────────────────
            var apis = await apiRepository.ListByServiceIdAsync(service.Id, cancellationToken);
            var references = await referenceRepository.ListByAssetAsync(
                serviceId, LinkedAssetType.Service, cancellationToken);
            var activeRefs = references.Where(r => r.IsActive).ToList();

            // ── 1. Ownership Score (10%) ─────────────────────────────
            var (ownershipScore, ownershipJustification) = ComputeOwnership(service);

            // ── 2. Documentation Score (10%) ─────────────────────────
            var (docScore, docJustification) = ComputeDocumentation(service, activeRefs);

            // ── 3. Contracts Score (15%) ─────────────────────────────
            var (contractsScore, contractsJustification) = await ComputeContracts(
                apis, contractsModule, cancellationToken);

            // ── 4. SLOs Score (15%) ──────────────────────────────────
            var (slosScore, slosJustification) = await ComputeSlos(
                request.ServiceName, request.Environment, reliabilityModule, cancellationToken);

            // ── 5. Observability Score (15%) ─────────────────────────
            var (obsScore, obsJustification) = ComputeObservability(service, apis);

            // ── 6. Change Governance Score (15%) ─────────────────────
            var (changeScore, changeJustification) = ComputeChangeGovernance(apis, activeRefs);

            // ── 7. Runbooks Score (10%) ──────────────────────────────
            var (runbooksScore, runbooksJustification) = await ComputeRunbooks(
                serviceId.ToString(), activeRefs, knowledgeModule, cancellationToken);

            // ── 8. Security Score (10%) ──────────────────────────────
            var (secScore, secJustification) = ComputeSecurity(service, apis);

            // ── Criar snapshot ───────────────────────────────────────
            var scorecard = ServiceScorecard.Create(
                request.ServiceName,
                service.TeamName,
                service.Domain,
                ownershipScore, ownershipJustification,
                docScore, docJustification,
                contractsScore, contractsJustification,
                slosScore, slosJustification,
                obsScore, obsJustification,
                changeScore, changeJustification,
                runbooksScore, runbooksJustification,
                secScore, secJustification);

            return new Response(
                ServiceName: scorecard.ServiceName,
                TeamName: scorecard.TeamName,
                Domain: scorecard.Domain,
                OverallScore: scorecard.OverallScore,
                MaturityLevel: scorecard.MaturityLevel,
                Dimensions: new DimensionScores(
                    Ownership: new DimensionDto(scorecard.OwnershipScore, scorecard.OwnershipJustification, 0.10m),
                    Documentation: new DimensionDto(scorecard.DocumentationScore, scorecard.DocumentationJustification, 0.10m),
                    Contracts: new DimensionDto(scorecard.ContractsScore, scorecard.ContractsJustification, 0.15m),
                    Slos: new DimensionDto(scorecard.SlosScore, scorecard.SlosJustification, 0.15m),
                    Observability: new DimensionDto(scorecard.ObservabilityScore, scorecard.ObservabilityJustification, 0.15m),
                    ChangeGovernance: new DimensionDto(scorecard.ChangeGovernanceScore, scorecard.ChangeGovernanceJustification, 0.15m),
                    Runbooks: new DimensionDto(scorecard.RunbooksScore, scorecard.RunbooksJustification, 0.10m),
                    Security: new DimensionDto(scorecard.SecurityScore, scorecard.SecurityJustification, 0.10m)),
                ComputedAt: scorecard.ComputedAt);
        }

        // ── Métodos de scoring ───────────────────────────────────────

        private static (decimal Score, string Justification) ComputeOwnership(ServiceAsset service)
        {
            var score = 0.0m;
            var factors = new List<string>();

            if (!string.IsNullOrWhiteSpace(service.TeamName))
            {
                score += 0.40m;
                factors.Add("Team assigned");
            }
            else
            {
                factors.Add("No team assigned (-0.40)");
            }

            if (!string.IsNullOrWhiteSpace(service.TechnicalOwner))
            {
                score += 0.30m;
                factors.Add("Technical owner defined");
            }
            else
            {
                factors.Add("No technical owner (-0.30)");
            }

            if (!string.IsNullOrWhiteSpace(service.BusinessOwner))
            {
                score += 0.30m;
                factors.Add("Business owner defined");
            }
            else
            {
                factors.Add("No business owner (-0.30)");
            }

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }

        private static (decimal Score, string Justification) ComputeDocumentation(
            ServiceAsset service, IReadOnlyList<LinkedReference> refs)
        {
            var score = 0.0m;
            var factors = new List<string>();

            if (!string.IsNullOrWhiteSpace(service.Description))
            {
                score += 0.25m;
                factors.Add("Description present");
            }
            else
            {
                factors.Add("No description (-0.25)");
            }

            if (!string.IsNullOrWhiteSpace(service.DocumentationUrl))
            {
                score += 0.25m;
                factors.Add("Documentation URL defined");
            }
            else
            {
                factors.Add("No documentation URL (-0.25)");
            }

            var docRefs = refs.Count(r => r.ReferenceType == LinkedReferenceType.Documentation);
            if (docRefs > 0)
            {
                score += 0.25m;
                factors.Add($"{docRefs} documentation reference(s) linked");
            }
            else
            {
                factors.Add("No documentation references (-0.25)");
            }

            if (!string.IsNullOrWhiteSpace(service.RepositoryUrl))
            {
                score += 0.25m;
                factors.Add("Repository URL defined");
            }
            else
            {
                factors.Add("No repository URL (-0.25)");
            }

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }

        private static async Task<(decimal Score, string Justification)> ComputeContracts(
            IReadOnlyList<ApiAsset> apis,
            IContractsModule contractsModule,
            CancellationToken ct)
        {
            var factors = new List<string>();

            if (apis.Count == 0)
            {
                return (0m, "No API assets registered for this service");
            }

            var score = 0.30m; // Base: at least one API exists
            factors.Add($"{apis.Count} API asset(s) registered");

            // Check if at least one contract version is published
            var hasAnyContract = false;
            decimal? bestContractScore = null;
            foreach (var api in apis)
            {
                var hasVersion = await contractsModule.HasContractVersionAsync(api.Id.Value, ct);
                if (hasVersion)
                {
                    hasAnyContract = true;
                    var cs = await contractsModule.GetLatestOverallScoreAsync(api.Id.Value, ct);
                    if (cs.HasValue && (!bestContractScore.HasValue || cs.Value > bestContractScore.Value))
                        bestContractScore = cs.Value;
                }
            }

            if (hasAnyContract)
            {
                score += 0.30m;
                factors.Add("Published contract version exists");

                if (bestContractScore.HasValue)
                {
                    // Map contract score (0-1) to contribution (0-0.40)
                    var contribution = bestContractScore.Value * 0.40m;
                    score += contribution;
                    factors.Add($"Best contract quality score: {bestContractScore.Value:P0}");
                }
            }
            else
            {
                factors.Add("No published contract versions (-0.30)");
            }

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }

        private static async Task<(decimal Score, string Justification)> ComputeSlos(
            string serviceName, string environment,
            IReliabilityModule reliabilityModule,
            CancellationToken ct)
        {
            var factors = new List<string>();

            var slos = await reliabilityModule.GetServiceSlosAsync(serviceName, environment, ct);

            if (slos.Count == 0)
            {
                return (0m, "No SLOs defined for this service/environment");
            }

            var score = 0.40m; // Base: SLOs exist
            factors.Add($"{slos.Count} SLO(s) defined");

            // Check reliability status
            var status = await reliabilityModule.GetCurrentReliabilityStatusAsync(serviceName, environment, ct);
            if (!string.IsNullOrEmpty(status))
            {
                score += status switch
                {
                    "Healthy" => 0.30m,
                    "AtRisk" => 0.15m,
                    _ => 0.05m
                };
                factors.Add($"Reliability status: {status}");
            }

            // Check error budget
            var budget = await reliabilityModule.GetRemainingErrorBudgetAsync(serviceName, environment, ct);
            if (budget.HasValue)
            {
                score += budget.Value > 0.50m ? 0.30m : budget.Value > 0 ? 0.15m : 0m;
                factors.Add($"Error budget remaining: {budget.Value:P0}");
            }

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }

        private static (decimal Score, string Justification) ComputeObservability(
            ServiceAsset service, IReadOnlyList<ApiAsset> apis)
        {
            // Observability is partially assessable from catalog data.
            // Full assessment requires ITelemetryQueryService (future integration).
            var score = 0.0m;
            var factors = new List<string>();

            // Active lifecycle implies some operational readiness
            var lifecycle = service.LifecycleStatus.ToString();
            if (lifecycle is "Active" or "Deprecating")
            {
                score += 0.30m;
                factors.Add("Service in active lifecycle");
            }
            else
            {
                factors.Add("Service not yet in active lifecycle (-0.30)");
            }

            // Having APIs suggests endpoints that can be monitored
            if (apis.Count > 0)
            {
                score += 0.20m;
                factors.Add($"{apis.Count} API endpoint(s) available for monitoring");
            }

            // Repository URL suggests CI/CD pipeline & instrumentation
            if (!string.IsNullOrWhiteSpace(service.RepositoryUrl))
            {
                score += 0.20m;
                factors.Add("Repository URL present (pipeline likely instrumented)");
            }

            // Note: full observability scoring awaits telemetry integration
            score += 0.10m; // Baseline for being registered
            factors.Add("Baseline: service registered in catalog");

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }

        private static (decimal Score, string Justification) ComputeChangeGovernance(
            IReadOnlyList<ApiAsset> apis,
            IReadOnlyList<LinkedReference> refs)
        {
            var score = 0.0m;
            var factors = new List<string>();

            // Changelog references indicate change tracking
            var changelogs = refs.Count(r => r.ReferenceType == LinkedReferenceType.Changelog);
            if (changelogs > 0)
            {
                score += 0.35m;
                factors.Add($"{changelogs} changelog reference(s)");
            }
            else
            {
                factors.Add("No changelog references (-0.35)");
            }

            // API assets with contracts enable change-level analysis
            if (apis.Count > 0)
            {
                score += 0.30m;
                factors.Add("API assets enable contract change tracking");
            }

            // Note: full change governance scoring requires IChangeIntelligenceModule
            // integration (release history, blast radius, promotion gates)
            score += 0.10m; // Baseline
            factors.Add("Baseline: change governance module available");

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }

        private static async Task<(decimal Score, string Justification)> ComputeRunbooks(
            string serviceId,
            IReadOnlyList<LinkedReference> refs,
            IKnowledgeModule knowledgeModule,
            CancellationToken ct)
        {
            var score = 0.0m;
            var factors = new List<string>();

            var runbookRefs = refs.Count(r => r.ReferenceType == LinkedReferenceType.Runbook);
            if (runbookRefs > 0)
            {
                score += 0.50m;
                factors.Add($"{runbookRefs} runbook reference(s) linked");
            }
            else
            {
                factors.Add("No runbook references (-0.50)");
            }

            var noteRefs = refs.Count(r => r.ReferenceType == LinkedReferenceType.OperationalNote);
            if (noteRefs > 0)
            {
                score += 0.20m;
                factors.Add($"{noteRefs} operational note(s)");
            }

            // Check knowledge module for documents
            var docCount = await knowledgeModule.CountDocumentsByServiceAsync(serviceId, ct);
            if (docCount > 0)
            {
                score += 0.30m;
                factors.Add($"{docCount} knowledge document(s) linked");
            }
            else
            {
                factors.Add("No knowledge documents (-0.30)");
            }

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }

        private static (decimal Score, string Justification) ComputeSecurity(
            ServiceAsset service, IReadOnlyList<ApiAsset> apis)
        {
            var score = 0.0m;
            var factors = new List<string>();

            // Active lifecycle suggests reviewed for security
            if (service.LifecycleStatus.ToString() is "Active")
            {
                score += 0.30m;
                factors.Add("Active lifecycle (security review implied)");
            }

            // Exposure type affects security requirements
            var exposure = service.ExposureType.ToString();
            if (!string.IsNullOrEmpty(exposure))
            {
                score += 0.20m;
                factors.Add($"Exposure type classified: {exposure}");
            }

            // Criticality classification
            var criticality = service.Criticality.ToString();
            if (!string.IsNullOrEmpty(criticality))
            {
                score += 0.20m;
                factors.Add($"Criticality defined: {criticality}");
            }

            // APIs count towards security posture (more attack surface, but documented)
            if (apis.Count > 0)
            {
                score += 0.15m;
                factors.Add($"{apis.Count} API(s) documented");
            }

            score += 0.10m; // Baseline for being in catalog
            factors.Add("Baseline: service registered");

            return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
        }
    }

    // ── Response DTOs ────────────────────────────────────────────────

    /// <summary>Resposta completa do scorecard de serviço.</summary>
    public sealed record Response(
        string ServiceName,
        string? TeamName,
        string? Domain,
        decimal OverallScore,
        string MaturityLevel,
        DimensionScores Dimensions,
        DateTimeOffset ComputedAt);

    /// <summary>Scores das 8 dimensões do scorecard.</summary>
    public sealed record DimensionScores(
        DimensionDto Ownership,
        DimensionDto Documentation,
        DimensionDto Contracts,
        DimensionDto Slos,
        DimensionDto Observability,
        DimensionDto ChangeGovernance,
        DimensionDto Runbooks,
        DimensionDto Security);

    /// <summary>Score e justificação de uma dimensão individual.</summary>
    public sealed record DimensionDto(
        decimal Score,
        string Justification,
        decimal Weight);
}
