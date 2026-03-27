using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;

/// <summary>
/// Retorna perfil de uso por persona.
/// Responde: quais personas usam quais capacidades? Qual a profundidade de uso?
/// Quais são os pontos de fricção e milestones atingidos por persona?
/// </summary>
public static class GetPersonaUsage
{
    /// <summary>Query para uso por persona com filtro opcional.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna o perfil de uso por persona.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var profiles = new List<PersonaUsageProfileDto>
            {
                new("Engineer", 98, 4_520,
                    new[] {
                        new PersonaModuleDto(ProductModule.SourceOfTruth, 92, 1_240),
                        new PersonaModuleDto(ProductModule.ContractStudio, 84, 980),
                        new PersonaModuleDto(ProductModule.AiAssistant, 78, 720),
                        new PersonaModuleDto(ProductModule.Search, 96, 1_580)
                    },
                    new[] { "search_executed", "entity_viewed", "contract_draft_created", "assistant_prompt_submitted" },
                    85.2m,
                    new[] { "zero_result_search", "empty_state_contracts" },
                    new[] { ValueMilestoneType.FirstSearchSuccess, ValueMilestoneType.FirstContractDraftCreated, ValueMilestoneType.FirstAiUsefulInteraction }),

                new("TechLead", 72, 2_840,
                    new[] {
                        new PersonaModuleDto(ProductModule.ChangeIntelligence, 88, 680),
                        new PersonaModuleDto(ProductModule.Reliability, 82, 520),
                        new PersonaModuleDto(ProductModule.Incidents, 76, 440),
                        new PersonaModuleDto(ProductModule.SourceOfTruth, 70, 380)
                    },
                    new[] { "change_viewed", "reliability_dashboard_viewed", "incident_investigated", "source_of_truth_queried" },
                    78.4m,
                    new[] { "navigation_loop_reliability", "aborted_journey_mitigation" },
                    new[] { ValueMilestoneType.FirstIncidentInvestigation, ValueMilestoneType.FirstMitigationCompleted }),

                new("Architect", 45, 1_620,
                    new[] {
                        new PersonaModuleDto(ProductModule.ContractStudio, 90, 520),
                        new PersonaModuleDto(ProductModule.SourceOfTruth, 86, 480),
                        new PersonaModuleDto(ProductModule.Governance, 62, 220),
                        new PersonaModuleDto(ProductModule.ChangeIntelligence, 58, 180)
                    },
                    new[] { "contract_published", "source_of_truth_queried", "policy_viewed", "change_viewed" },
                    72.1m,
                    new[] { "empty_state_policies", "late_discovery_evidence" },
                    new[] { ValueMilestoneType.FirstContractPublished, ValueMilestoneType.FirstSourceOfTruthUsed }),

                new("Product", 18, 980,
                    new[] {
                        new PersonaModuleDto(ProductModule.ExecutiveViews, 92, 340),
                        new PersonaModuleDto(ProductModule.Governance, 78, 280),
                        new PersonaModuleDto(ProductModule.FinOps, 65, 180),
                        new PersonaModuleDto(ProductModule.Search, 88, 180)
                    },
                    new[] { "executive_overview_viewed", "report_generated", "search_executed" },
                    68.5m,
                    new[] { "empty_state_reports" },
                    new[] { ValueMilestoneType.FirstExecutiveOverviewConsumed, ValueMilestoneType.FirstReportGenerated }),

                new("Executive", 12, 420,
                    new[] {
                        new PersonaModuleDto(ProductModule.ExecutiveViews, 96, 280),
                        new PersonaModuleDto(ProductModule.FinOps, 42, 80),
                        new PersonaModuleDto(ProductModule.Governance, 38, 60)
                    },
                    new[] { "executive_overview_viewed", "report_generated" },
                    82.3m,
                    new[] { "navigation_loop_reports" },
                    new[] { ValueMilestoneType.FirstExecutiveOverviewConsumed }),

                new("PlatformAdmin", 8, 580,
                    new[] {
                        new PersonaModuleDto(ProductModule.Admin, 94, 280),
                        new PersonaModuleDto(ProductModule.IntegrationHub, 86, 180),
                        new PersonaModuleDto(ProductModule.Governance, 72, 120)
                    },
                    new[] { "policy_viewed", "connector_configured", "user_managed" },
                    71.8m,
                    new[] { "quota_exceeded", "blocked_by_policy" },
                    new[] { ValueMilestoneType.FirstSourceOfTruthUsed }),

                new("Auditor", 6, 320,
                    new[] {
                        new PersonaModuleDto(ProductModule.Governance, 92, 180),
                        new PersonaModuleDto(ProductModule.ExecutiveViews, 68, 80),
                        new PersonaModuleDto(ProductModule.SourceOfTruth, 54, 60)
                    },
                    new[] { "evidence_package_exported", "policy_viewed", "compliance_check_viewed" },
                    75.4m,
                    new[] { "empty_state_evidence" },
                    new[] { ValueMilestoneType.FirstEvidenceExported })
            };

            // Filtrar por persona se especificado
            if (!string.IsNullOrWhiteSpace(request.Persona))
            {
                profiles = profiles
                    .Where(p => p.Persona.Equals(request.Persona, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var response = new Response(
                Profiles: profiles,
                TotalPersonas: 7,
                MostActivePersona: "Engineer",
                DeepestAdoptionPersona: "Engineer",
                PeriodLabel: request.Range ?? "last_30d");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com perfis de uso por persona.</summary>
    public sealed record Response(
        IReadOnlyList<PersonaUsageProfileDto> Profiles,
        int TotalPersonas,
        string MostActivePersona,
        string DeepestAdoptionPersona,
        string PeriodLabel);

    /// <summary>Perfil de uso de uma persona específica.</summary>
    public sealed record PersonaUsageProfileDto(
        string Persona,
        int ActiveUsers,
        long TotalActions,
        IReadOnlyList<PersonaModuleDto> TopModules,
        IReadOnlyList<string> TopActions,
        decimal AdoptionDepth,
        IReadOnlyList<string> CommonFrictionPoints,
        IReadOnlyList<ValueMilestoneType> MilestonesReached);

    /// <summary>Uso de módulo por persona.</summary>
    public sealed record PersonaModuleDto(
        ProductModule Module,
        int AdoptionPercent,
        long ActionCount);
}
