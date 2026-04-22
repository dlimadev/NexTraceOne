using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.GetPersonaConfig;

/// <summary>
/// Feature: GetPersonaConfig — retorna a configuração de navegação adaptativa para a persona
/// do utilizador autenticado. Alimenta o frontend com quick actions e módulos priorizados
/// sem depender de lógica de permissão no browser.
/// </summary>
public static class GetPersonaConfig
{
    /// <summary>Query sem parâmetros; a persona é derivada do ICurrentUser.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Handler que deriva a persona e retorna a configuração de navegação.</summary>
    public sealed class Handler(ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
            {
                return Task.FromResult<Result<Response>>(IdentityErrors.NotAuthenticated());
            }

            var persona = DerivePersona(currentUser.Persona);
            var (quickActions, prioritizedModules) = BuildConfig(persona);

            return Task.FromResult<Result<Response>>(
                new Response(persona, quickActions, prioritizedModules));
        }

        /// <summary>
        /// Deriva a persona a partir do claim explícito presente no ICurrentUser.
        /// Quando o claim x-nxt-persona está presente, tem prioridade absoluta.
        /// Fallback: "Engineer".
        /// </summary>
        private static string DerivePersona(string? personaClaim)
        {
            if (!string.IsNullOrWhiteSpace(personaClaim))
            {
                return personaClaim;
            }

            return "Engineer";
        }

        /// <summary>Constrói quick actions e módulos priorizados por persona.</summary>
        private static (IReadOnlyList<QuickActionDto> QuickActions, IReadOnlyList<string> PrioritizedModules) BuildConfig(
            string persona)
        {
            return persona switch
            {
                "Engineer" => (
                    [
                        new("investigate-service", "persona.Engineer.actions.investigateService", "Search", "/services"),
                        new("open-runbook", "persona.Engineer.actions.openRunbook", "FileCode", "/operations/runbooks"),
                        new("view-contract", "persona.Engineer.actions.viewContract", "FileText", "/contracts"),
                        new("analyze-incident", "persona.Engineer.actions.analyzeIncident", "AlertTriangle", "/operations/incidents"),
                        new("ai-assistant", "persona.Engineer.actions.aiAssistant", "Bot", "/ai/assistant"),
                    ],
                    ["services", "operations", "changes", "contracts", "knowledge"]),

                "TechLead" => (
                    [
                        new("review-team-risk", "persona.TechLead.actions.reviewTeamRisk", "ShieldAlert", "/governance/risk"),
                        new("inspect-changes", "persona.TechLead.actions.inspectChanges", "Zap", "/changes"),
                        new("assign-owner", "persona.TechLead.actions.assignOwner", "UserCheck", "/services"),
                        new("open-team-services", "persona.TechLead.actions.openTeamServices", "Server", "/services"),
                        new("view-slo", "persona.TechLead.actions.viewSlo", "Activity", "/operations/reliability"),
                    ],
                    ["services", "changes", "operations", "organization", "governance"]),

                "Architect" => (
                    [
                        new("inspect-deps", "persona.Architect.actions.inspectDependencyMap", "Share2", "/services/graph"),
                        new("review-cross-impact", "persona.Architect.actions.reviewCrossImpact", "Zap", "/changes"),
                        new("analyze-contracts", "persona.Architect.actions.analyzeContractCompatibility", "FileText", "/contracts"),
                        new("source-of-truth", "persona.Architect.actions.sourceOfTruth", "Globe", "/source-of-truth"),
                        new("coupling-index", "persona.Architect.actions.couplingIndex", "Network", "/services/graph"),
                    ],
                    ["services", "contracts", "knowledge", "changes", "governance"]),

                "Product" => (
                    [
                        new("review-release", "persona.Product.actions.reviewReleaseConfidence", "ShieldCheck", "/changes"),
                        new("critical-status", "persona.Product.actions.viewCriticalServiceStatus", "Activity", "/operations/reliability"),
                        new("product-incidents", "persona.Product.actions.inspectProductIncidents", "AlertTriangle", "/operations/incidents"),
                        new("view-reports", "persona.Product.actions.viewReports", "BarChart3", "/governance/reports"),
                        new("finops-burn", "persona.Product.actions.finopsBurn", "DollarSign", "/governance/finops"),
                    ],
                    ["analytics", "changes", "services", "operations", "governance"]),

                "Executive" => (
                    [
                        new("executive-dashboard", "persona.Executive.actions.executiveDashboard", "LayoutDashboard", "/governance/executive/intelligence"),
                        new("executive-overview", "persona.Executive.actions.openExecutiveOverview", "BarChart3", "/governance/executive"),
                        new("operational-trend", "persona.Executive.actions.reviewOperationalTrend", "Activity", "/governance/risk"),
                        new("view-compliance", "persona.Executive.actions.viewCompliance", "Scale", "/governance/compliance"),
                        new("finops-burn", "persona.Executive.actions.finopsBurn", "DollarSign", "/governance/finops"),
                    ],
                    ["governance", "analytics", "organization", "changes", "services"]),

                "PlatformAdmin" => (
                    [
                        new("manage-policies", "persona.PlatformAdmin.actions.managePolicies", "ShieldCheck", "/ai/policies"),
                        new("manage-models", "persona.PlatformAdmin.actions.manageAiModels", "Database", "/ai/models"),
                        new("configure-integrations", "persona.PlatformAdmin.actions.configureIntegrations", "Settings", "/integrations"),
                        new("review-coverage", "persona.PlatformAdmin.actions.reviewPlatformCoverage", "BarChart3", "/governance/reports"),
                        new("system-health", "persona.PlatformAdmin.actions.systemHealth", "Heart", "/admin/system-health"),
                    ],
                    ["admin", "organization", "integrations", "governance", "aiHub"]),

                "Auditor" => (
                    [
                        new("inspect-audit", "persona.Auditor.actions.inspectAuditTrail", "ClipboardList", "/audit"),
                        new("export-evidence", "persona.Auditor.actions.exportEvidence", "Download", "/audit"),
                        new("review-approvals", "persona.Auditor.actions.reviewApprovalHistory", "ClipboardCheck", "/governance/compliance"),
                        new("ai-audit", "persona.Auditor.actions.auditAiUsage", "Bot", "/ai/policies"),
                        new("compliance-matrix", "persona.Auditor.actions.complianceMatrix", "CheckSquare", "/governance/compliance"),
                    ],
                    ["governance", "admin", "changes", "operations", "organization"]),

                _ => (
                    [
                        new("investigate-service", "persona.Engineer.actions.investigateService", "Search", "/services"),
                        new("view-contract", "persona.Engineer.actions.viewContract", "FileText", "/contracts"),
                        new("analyze-incident", "persona.Engineer.actions.analyzeIncident", "AlertTriangle", "/operations/incidents"),
                    ],
                    ["services", "operations", "changes"]),
            };
        }
    }

    /// <summary>Resposta com configuração de navegação adaptativa para a persona.</summary>
    public sealed record Response(
        string Persona,
        IReadOnlyList<QuickActionDto> QuickActions,
        IReadOnlyList<string> PrioritizedModules);

    /// <summary>Quick action com rota e metadados de i18n.</summary>
    public sealed record QuickActionDto(
        string Id,
        string LabelKey,
        string Icon,
        string To);
}
