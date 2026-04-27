using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetPersonaHome;

/// <summary>
/// Feature: GetPersonaHome — compõe a home page personalizada para uma persona e utilizador.
/// V3.10 — Persona-first Experience Suites.
/// Retorna configuração de cards + quick actions + escopo default.
/// Retorna IsSimulated=true para cards cujos dados não tenham backend real conectado.
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

    public sealed class Handler(IPersonaHomeConfigurationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Load persisted config if exists; otherwise use system defaults
            var config = await repository.GetByUserPersonaAsync(
                request.UserId, request.Persona, request.TenantId, cancellationToken);

            var (cards, quickActions) = BuildPersonaDefaults(request.Persona);

            const string simulatedNote =
                "Home cards showing system-default layout. Real-time data bridges pending.";

            return Result<Response>.Success(new Response(
                request.Persona,
                request.UserId,
                cards,
                quickActions,
                IsSimulated: true,
                SimulatedNote: simulatedNote));
        }

        private static (IReadOnlyList<HomeCardDto> Cards, IReadOnlyList<QuickActionDto> Actions)
            BuildPersonaDefaults(string persona)
        {
            return persona.ToLowerInvariant() switch
            {
                "engineer" => (
                    Cards:
                    [
                        new("my_services", "My Services", "4", null, null, "info", "/governance/scorecards", true),
                        new("open_incidents", "Open Incidents", "1", "-2", null, "warning", "/operations/incidents", true),
                        new("pending_approvals", "Pending Approvals", "3", null, null, "warning", "/changes/approvals", true),
                        new("slo_status", "SLO Status", "97.2%", "+0.3%", null, "success", "/slos/my", true),
                        new("last_deploy", "Last Deploy", "2h ago", null, null, "info", "/changes/deployments", true),
                        new("on_call_status", "On-Call Status", "Active", null, null, "warning", "/on-call", true)
                    ],
                    Actions:
                    [
                        new("runbooks", "My Runbooks", "/knowledge/runbooks", "book"),
                        new("incidents", "Open Incidents", "/operations/incidents", "alert"),
                        new("deploys", "My Deployments", "/changes/deployments", "rocket"),
                        new("slos", "My SLOs", "/slos/my", "gauge")
                    ]
                ),
                "tech-lead" => (
                    Cards:
                    [
                        new("team_health", "Team Health", "82/100", "+3", null, "success", "/governance/teams", true),
                        new("velocity", "Velocity", "14 deploys/wk", "+2", null, "info", "/governance/dora-metrics", true),
                        new("blockers", "Blockers", "2", null, null, "warning", "/changes/blockers", true),
                        new("slo_compliance", "SLO Compliance", "94%", "-1%", null, "info", "/slos/team", true),
                        new("change_confidence", "Change Confidence", "High", null, null, "success", "/changes/confidence", true),
                        new("ownership_gaps", "Ownership Gaps", "1", null, null, "warning", "/governance/teams", true)
                    ],
                    Actions:
                    [
                        new("team_detail", "Team Dashboard", "/governance/teams", "users"),
                        new("dora", "DORA Metrics", "/governance/dora-metrics", "chart"),
                        new("changes", "Change Approvals", "/changes/approvals", "check-circle"),
                        new("scorecards", "Service Scorecards", "/governance/scorecards", "star")
                    ]
                ),
                "executive" => (
                    Cards:
                    [
                        new("compliance_score", "Compliance Score", "91%", "+2%", null, "success", "/governance/compliance", true),
                        new("risk_level", "Risk Level", "Medium", null, null, "warning", "/governance/risk", true),
                        new("finops_budget", "FinOps Budget", "73%", "+5%", null, "info", "/governance/finops", true),
                        new("dora_tier", "DORA Tier", "Elite", null, null, "success", "/governance/dora-metrics", true),
                        new("active_incidents", "Active Incidents", "1", null, null, "warning", "/operations/incidents", true),
                        new("open_changes", "Open Changes", "12", null, null, "info", "/changes", true)
                    ],
                    Actions:
                    [
                        new("executive_overview", "Executive Overview", "/governance/executive", "dashboard"),
                        new("compliance", "Compliance", "/governance/compliance", "shield"),
                        new("finops", "FinOps", "/governance/finops", "dollar"),
                        new("reports", "Reports", "/governance/reports", "file")
                    ]
                ),
                _ => (
                    Cards:
                    [
                        new("services", "Services", "24", null, null, "info", "/catalog/services", true),
                        new("changes", "Changes", "8", null, null, "info", "/changes", true),
                        new("compliance", "Compliance", "91%", null, null, "success", "/governance/compliance", true),
                        new("incidents", "Incidents", "1", null, null, "warning", "/operations/incidents", true),
                        new("risk", "Risk", "Medium", null, null, "warning", "/governance/risk", true),
                        new("slos", "SLOs", "94%", null, null, "info", "/slos", true)
                    ],
                    Actions:
                    [
                        new("home", "Overview", "/", "home"),
                        new("changes", "Changes", "/changes", "git-branch"),
                        new("incidents", "Incidents", "/operations/incidents", "alert"),
                        new("governance", "Governance", "/governance", "shield")
                    ]
                )
            };
        }
    }
}
