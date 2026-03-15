using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetOnboardingContext;

/// <summary>
/// Feature: GetOnboardingContext — devolve contexto de onboarding adaptado à persona do utilizador.
/// Inclui quickstart steps, ações recomendadas e dicas de discoverability por persona.
/// </summary>
public static class GetOnboardingContext
{
    /// <summary>Query para obter o contexto de onboarding. Persona determina os itens devolvidos.</summary>
    public sealed record Query(string Persona) : IQuery<Response>;

    /// <summary>Handler que computa o contexto de onboarding por persona.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var persona = request.Persona ?? "Engineer";
            var quickstartItems = BuildQuickstart(persona);
            var recommendations = BuildRecommendations(persona);

            var response = new Response(
                Persona: persona,
                QuickstartItems: quickstartItems,
                RecommendedActions: recommendations,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static IReadOnlyList<QuickstartItem> BuildQuickstart(string persona)
        {
            return persona switch
            {
                "Engineer" =>
                [
                    new("qs-svc", "Explore Service Catalog", "/services", "services", 1),
                    new("qs-changes", "Review Recent Changes", "/changes", "changes", 2),
                    new("qs-incidents", "Check Open Incidents", "/operations/incidents", "incidents", 3),
                    new("qs-contracts", "Browse API Contracts", "/contracts", "contracts", 4),
                ],
                "TechLead" =>
                [
                    new("qs-team-svc", "Review Team Services", "/services", "services", 1),
                    new("qs-change-risk", "Check Change Risk", "/changes", "changes", 2),
                    new("qs-reliability", "Monitor Reliability", "/operations/reliability", "reliability", 3),
                    new("qs-incidents", "Review Team Incidents", "/operations/incidents", "incidents", 4),
                ],
                "Architect" =>
                [
                    new("qs-deps", "Explore Dependency Graph", "/services/graph", "dependencies", 1),
                    new("qs-contracts", "Review Contract Consistency", "/contracts", "contracts", 2),
                    new("qs-cross-changes", "Analyze Cross-Team Changes", "/changes", "changes", 3),
                    new("qs-risk", "Check Architectural Risk", "/governance/risk", "risk", 4),
                ],
                "Product" =>
                [
                    new("qs-confidence", "Review Release Confidence", "/changes", "releaseConfidence", 1),
                    new("qs-critical", "Check Critical Services", "/operations/reliability", "services", 2),
                    new("qs-risk", "Monitor Operational Risk", "/governance/risk", "risk", 3),
                    new("qs-incidents", "View Recent Incidents", "/operations/incidents", "incidents", 4),
                ],
                "Executive" =>
                [
                    new("qs-overview", "Open Executive Overview", "/governance/executive", "executive", 1),
                    new("qs-risk", "Review Risk Trends", "/governance/risk", "risk", 2),
                    new("qs-domains", "Inspect Critical Domains", "/services", "domains", 3),
                    new("qs-compliance", "Check Compliance", "/governance/compliance", "compliance", 4),
                ],
                "PlatformAdmin" =>
                [
                    new("qs-policies", "Review Policies", "/governance/policies", "policies", 1),
                    new("qs-models", "Manage AI Models", "/ai/models", "aiModels", 2),
                    new("qs-integrations", "Configure Integrations", "/users", "integrations", 3),
                    new("qs-coverage", "Check Platform Coverage", "/governance/reports", "coverage", 4),
                ],
                "Auditor" =>
                [
                    new("qs-audit", "Inspect Audit Trail", "/audit", "audit", 1),
                    new("qs-evidence", "Review Evidence Packages", "/governance/evidence", "evidence", 2),
                    new("qs-approvals", "Check Approval History", "/governance/compliance", "approvals", 3),
                    new("qs-ai-usage", "Audit AI Usage", "/ai/policies", "aiUsage", 4),
                ],
                _ =>
                [
                    new("qs-svc", "Explore Service Catalog", "/services", "services", 1),
                    new("qs-contracts", "Browse API Contracts", "/contracts", "contracts", 2),
                    new("qs-changes", "Review Recent Changes", "/changes", "changes", 3),
                    new("qs-incidents", "Check Incidents", "/operations/incidents", "incidents", 4),
                ],
            };
        }

        private static IReadOnlyList<RecommendedAction> BuildRecommendations(string persona)
        {
            return persona switch
            {
                "Engineer" =>
                [
                    new("rec-runbook", "Create a Runbook", "/operations/runbooks", "Create operational procedures for your services"),
                    new("rec-contract", "Define API Contract", "/contracts/studio", "Use Contract Studio to define service contracts"),
                ],
                "TechLead" =>
                [
                    new("rec-ownership", "Review Ownership Gaps", "/services", "Ensure all services have assigned owners"),
                    new("rec-reliability", "Set SLO Targets", "/operations/reliability", "Define reliability targets for team services"),
                ],
                "Architect" =>
                [
                    new("rec-sot", "Explore Source of Truth", "/source-of-truth", "Central reference for all services and contracts"),
                    new("rec-deps", "Review Dependencies", "/services/graph", "Understand service topology and coupling"),
                ],
                _ =>
                [
                    new("rec-explore", "Explore the Platform", "/services", "Start by reviewing registered services"),
                ],
            };
        }
    }

    /// <summary>Resposta do contexto de onboarding.</summary>
    public sealed record Response(
        string Persona,
        IReadOnlyList<QuickstartItem> QuickstartItems,
        IReadOnlyList<RecommendedAction> RecommendedActions,
        DateTimeOffset GeneratedAt);

    /// <summary>Item de quickstart — passo recomendado para o utilizador.</summary>
    public sealed record QuickstartItem(
        string Id,
        string Label,
        string Route,
        string Scope,
        int Order);

    /// <summary>Acção recomendada por contexto.</summary>
    public sealed record RecommendedAction(
        string Id,
        string Label,
        string Route,
        string Description);
}
