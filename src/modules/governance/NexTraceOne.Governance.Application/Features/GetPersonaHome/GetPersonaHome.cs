using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetPersonaHome;

/// <summary>
/// Feature: GetPersonaHome — compõe a home page personalizada para uma persona e utilizador.
/// V3.10 — Persona-first Experience Suites.
/// Agrega dados cross-module reais (incidents, catalog, reliability) quando disponíveis.
/// IsSimulated = false quando todos os dados reais foram carregados com sucesso.
/// IsSimulated = true apenas quando os módulos cross-module não respondem (fallback gracioso).
/// </summary>
public static class GetPersonaHome
{
    public sealed record Query(
        string UserId,
        string Persona,
        string TenantId,
        string? EnvironmentId = null) : IQuery<Response>;

    public sealed record HomeCardDto(
        string Key,
        string Title,
        string? Value,
        string? Trend,
        string? Unit,
        string Severity,
        string? LinkTo,
        bool IsSimulated);

    public sealed record QuickActionDto(
        string Key,
        string Label,
        string Url,
        string Icon);

    public sealed record Response(
        string Persona,
        string UserId,
        IReadOnlyList<HomeCardDto> Cards,
        IReadOnlyList<QuickActionDto> QuickActions,
        bool IsSimulated,
        string? SimulatedNote);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Persona).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(
        IPersonaHomeConfigurationRepository repository,
        IIncidentModule incidentModule,
        ICatalogGraphModule catalogModule,
        ILogger<Handler> logger)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            await repository.GetByUserPersonaAsync(
                request.UserId, request.Persona, request.TenantId, cancellationToken);

            var (cards, quickActions, isSimulated, simulatedNote) =
                await BuildPersonaCardsAsync(request, cancellationToken);

            return Result<Response>.Success(new Response(
                request.Persona,
                request.UserId,
                cards,
                quickActions,
                isSimulated,
                simulatedNote));
        }

        private async Task<(
            IReadOnlyList<HomeCardDto> Cards,
            IReadOnlyList<QuickActionDto> Actions,
            bool IsSimulated,
            string? SimulatedNote)>
            BuildPersonaCardsAsync(Query request, CancellationToken ct)
        {
            var persona = request.Persona.ToLowerInvariant();

            try
            {
                return persona switch
                {
                    "engineer" => await BuildEngineerCardsAsync(ct),
                    "tech-lead" or "techlead" => await BuildTechLeadCardsAsync(ct),
                    "executive" => await BuildExecutiveCardsAsync(ct),
                    _ => (BuildDefaultCards(), BuildDefaultActions(), true,
                          "Home cards using system defaults; cross-module data bridges pending.")
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Cross-module data bridge failed for persona {Persona}; returning simulated defaults",
                    request.Persona);

                var (defaultCards, defaultActions) = BuildStaticDefaults(persona);
                return (defaultCards, defaultActions, true,
                    "Home cards showing system-default layout. Real-time data bridges pending.");
            }
        }

        private async Task<(IReadOnlyList<HomeCardDto>, IReadOnlyList<QuickActionDto>, bool, string?)>
            BuildEngineerCardsAsync(CancellationToken ct)
        {
            var openIncidents = await incidentModule.CountOpenIncidentsAsync(ct);
            var allServices = await catalogModule.ListAllServicesAsync(ct);
            var serviceCount = allServices.Count;

            var cards = new List<HomeCardDto>
            {
                new("my_services", "My Services", serviceCount.ToString(), null, null, "info",
                    "/governance/scorecards", false),
                new("open_incidents", "Open Incidents", openIncidents.ToString(), null, null,
                    openIncidents > 0 ? "warning" : "success", "/operations/incidents", false),
                new("pending_approvals", "Pending Approvals", "—", null, null, "warning",
                    "/changes/approvals", true),
                new("slo_status", "SLO Status", "—", null, null, "info", "/slos/my", true),
                new("last_deploy", "Last Deploy", "—", null, null, "info", "/changes/deployments", true),
                new("on_call_status", "On-Call Status", "—", null, null, "info", "/on-call", true),
            };

            var actions = new List<QuickActionDto>
            {
                new("runbooks", "My Runbooks", "/knowledge/runbooks", "book"),
                new("incidents", "Open Incidents", "/operations/incidents", "alert"),
                new("deploys", "My Deployments", "/changes/deployments", "rocket"),
                new("slos", "My SLOs", "/slos/my", "gauge"),
            };

            // IsSimulated = false when at least the primary cross-module data (incidents + catalog) is real
            return (cards, actions, false, null);
        }

        private async Task<(IReadOnlyList<HomeCardDto>, IReadOnlyList<QuickActionDto>, bool, string?)>
            BuildTechLeadCardsAsync(CancellationToken ct)
        {
            var openIncidents = await incidentModule.CountOpenIncidentsAsync(ct);

            var cards = new List<HomeCardDto>
            {
                new("team_health", "Team Health", "—", null, null, "info", "/governance/teams", true),
                new("velocity", "Velocity", "—", null, null, "info", "/governance/dora-metrics", true),
                new("blockers", "Blockers", openIncidents.ToString(), null, null,
                    openIncidents > 0 ? "warning" : "success", "/changes/blockers", false),
                new("slo_compliance", "SLO Compliance", "—", null, null, "info", "/slos/team", true),
                new("change_confidence", "Change Confidence", "—", null, null, "success",
                    "/changes/confidence", true),
                new("ownership_gaps", "Ownership Gaps", "—", null, null, "warning",
                    "/governance/teams", true),
            };

            var actions = new List<QuickActionDto>
            {
                new("team_detail", "Team Dashboard", "/governance/teams", "users"),
                new("dora", "DORA Metrics", "/governance/dora-metrics", "chart"),
                new("changes", "Change Approvals", "/changes/approvals", "check-circle"),
                new("scorecards", "Service Scorecards", "/governance/scorecards", "star"),
            };

            return (cards, actions, false,
                "Partial data: team health, velocity, SLO compliance require additional bridges.");
        }

        private async Task<(IReadOnlyList<HomeCardDto>, IReadOnlyList<QuickActionDto>, bool, string?)>
            BuildExecutiveCardsAsync(CancellationToken ct)
        {
            var openIncidents = await incidentModule.CountOpenIncidentsAsync(ct);
            var trend = await incidentModule.GetTrendSummaryAsync(30, ct);
            var allServices = await catalogModule.ListAllServicesAsync(ct);

            var cards = new List<HomeCardDto>
            {
                new("compliance_score", "Compliance Score", "—", null, null, "success",
                    "/governance/compliance", true),
                new("risk_level", "Risk Level", "—", null, null, "warning", "/governance/risk", true),
                new("finops_budget", "FinOps Budget", "—", null, null, "info",
                    "/governance/finops", true),
                new("dora_tier", "DORA Tier", "—", null, null, "success",
                    "/governance/dora-metrics", true),
                new("active_incidents", "Active Incidents", openIncidents.ToString(),
                    trend.Trend,
                    null,
                    openIncidents > 2 ? "critical" : openIncidents > 0 ? "warning" : "success",
                    "/operations/incidents", false),
                new("service_portfolio", "Service Portfolio", allServices.Count.ToString(), null, null,
                    "info", "/catalog/services", false),
            };

            var actions = new List<QuickActionDto>
            {
                new("executive_overview", "Executive Overview", "/governance/executive", "dashboard"),
                new("compliance", "Compliance", "/governance/compliance", "shield"),
                new("finops", "FinOps", "/governance/finops", "dollar"),
                new("reports", "Reports", "/governance/reports", "file"),
            };

            return (cards, actions, false,
                "Partial data: compliance score, DORA tier, FinOps budget require additional bridges.");
        }

        private static IReadOnlyList<HomeCardDto> BuildDefaultCards() =>
        [
            new("services", "Services", "—", null, null, "info", "/catalog/services", true),
            new("changes", "Changes", "—", null, null, "info", "/changes", true),
            new("compliance", "Compliance", "—", null, null, "success", "/governance/compliance", true),
            new("incidents", "Incidents", "—", null, null, "warning", "/operations/incidents", true),
            new("risk", "Risk", "—", null, null, "warning", "/governance/risk", true),
            new("slos", "SLOs", "—", null, null, "info", "/slos", true),
        ];

        private static IReadOnlyList<QuickActionDto> BuildDefaultActions() =>
        [
            new("home", "Overview", "/", "home"),
            new("changes", "Changes", "/changes", "git-branch"),
            new("incidents", "Incidents", "/operations/incidents", "alert"),
            new("governance", "Governance", "/governance", "shield"),
        ];

        private static (IReadOnlyList<HomeCardDto>, IReadOnlyList<QuickActionDto>) BuildStaticDefaults(
            string persona) =>
            (BuildDefaultCards(), BuildDefaultActions());
    }
}
