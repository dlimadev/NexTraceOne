using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ComposeAiDashboard;

/// <summary>
/// Feature: ComposeAiDashboard — AI Agent "Dashboard Composer".
/// Recebe um prompt em linguagem natural + contexto (persona, equipa, env, serviços)
/// e devolve uma PROPOSTA estruturada de dashboard (variáveis, layout, widgets, NQL)
/// como DRAFT — nunca aplica sem aprovação humana.
///
/// Honest gap: o grounding real em Catalog/Contracts/Changes/Incidents e a invocação
/// de LLM requerem bridges que serão ligadas na camada de infraestrutura.
/// O handler atual devolve IsSimulated=true + proposta estruturada determinística
/// baseada em keyword analysis do prompt para uso no frontend sem um LLM ativo.
///
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public static class ComposeAiDashboard
{
    public sealed record Command(
        string Prompt,
        string TenantId,
        string UserId,
        string Persona,
        string? TeamId,
        string? EnvironmentId,
        IReadOnlyList<string>? ServiceIds) : ICommand<Response>;

    public sealed record ProposedVariableDto(
        string Key,
        string Label,
        string Type,
        string? DefaultValue);

    public sealed record ProposedWidgetDto(
        string WidgetType,
        string? Title,
        string? ServiceFilter,
        string? NqlQuery,
        int GridX,
        int GridY,
        int GridWidth,
        int GridHeight);

    public sealed record Response(
        bool IsSimulated,
        string? SimulatedNote,
        string ProposedTitle,
        string ProposedLayout,
        IReadOnlyList<ProposedVariableDto> ProposedVariables,
        IReadOnlyList<ProposedWidgetDto> ProposedWidgets,
        string GroundingContext,
        DateTimeOffset GeneratedAt);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Prompt).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Persona).NotEmpty();
        }
    }

    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Honest-gap: keyword analysis generates a deterministic proposal
            // until AI bridge is connected in infrastructure.
            var prompt = request.Prompt.ToLowerInvariant();

            var title = InferTitle(prompt, request.Persona);
            var variables = BuildVariables(request.Persona, request.TeamId, request.EnvironmentId);
            var widgets = BuildWidgets(prompt, request.Persona, request.ServiceIds);

            var response = new Response(
                IsSimulated: true,
                SimulatedNote: "Dashboard proposal generated via keyword analysis. Connect AI model for LLM-grounded composition.",
                ProposedTitle: title,
                ProposedLayout: "grid",
                ProposedVariables: variables,
                ProposedWidgets: widgets,
                GroundingContext: BuildGroundingContext(request),
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static string InferTitle(string prompt, string persona)
        {
            if (prompt.Contains("slo") || prompt.Contains("reliability"))
                return "SLO & Reliability Dashboard";
            if (prompt.Contains("incident") || prompt.Contains("on-call") || prompt.Contains("oncall"))
                return "Incident & On-Call Overview";
            if (prompt.Contains("cost") || prompt.Contains("finops") || prompt.Contains("budget"))
                return "FinOps Cost Intelligence";
            if (prompt.Contains("change") || prompt.Contains("deploy") || prompt.Contains("release"))
                return "Change & Release Intelligence";
            if (prompt.Contains("compliance") || prompt.Contains("audit") || prompt.Contains("risk"))
                return "Compliance & Risk Center";
            if (prompt.Contains("team") || prompt.Contains("squad"))
                return $"{persona} Team Health Dashboard";
            return $"{persona} Overview Dashboard";
        }

        private static IReadOnlyList<ProposedVariableDto> BuildVariables(
            string persona, string? teamId, string? envId)
        {
            var vars = new List<ProposedVariableDto>
            {
                new("$env", "Environment", "env", envId ?? "production"),
                new("$timeRange", "Time Range", "timeRange", "7d"),
                new("$service", "Service", "service", null),
            };
            if (!string.IsNullOrWhiteSpace(teamId))
                vars.Insert(0, new("$team", "Team", "team", teamId));
            return vars;
        }

        private static IReadOnlyList<ProposedWidgetDto> BuildWidgets(
            string prompt, string persona, IReadOnlyList<string>? serviceIds)
        {
            var widgets = new List<ProposedWidgetDto>();
            var serviceFilter = serviceIds is { Count: > 0 } ? serviceIds[0] : null;

            var row = 0;

            // Always include a service health overview
            widgets.Add(new("service-scorecard", "Service Health", serviceFilter, null, 0, row, 6, 4));

            if (prompt.Contains("slo") || prompt.Contains("reliability") || persona is "Engineer" or "TechLead")
            {
                widgets.Add(new("slo-gauge", "SLO Compliance", serviceFilter, null, 6, row, 6, 4));
                row += 4;
                widgets.Add(new("reliability-slo", "Error Budget Burn", serviceFilter, null, 0, row, 8, 4));
            }
            else
            {
                row += 4;
            }

            if (prompt.Contains("incident") || prompt.Contains("on-call"))
            {
                widgets.Add(new("incident-summary", "Active Incidents", serviceFilter, null, 0, row, 6, 4));
                widgets.Add(new("alert-status", "Alert Status", serviceFilter, null, 6, row, 6, 4));
                row += 4;
            }

            if (prompt.Contains("change") || prompt.Contains("deploy") || prompt.Contains("release"))
            {
                widgets.Add(new("change-confidence", "Change Confidence", serviceFilter, null, 0, row, 6, 4));
                widgets.Add(new("deployment-frequency", "Deployment Frequency", serviceFilter, null, 6, row, 6, 4));
                row += 4;
            }

            if (prompt.Contains("cost") || prompt.Contains("finops"))
            {
                widgets.Add(new("cost-trend", "Cost Trend", serviceFilter, null, 0, row, 8, 4));
                row += 4;
            }

            if (prompt.Contains("dora"))
            {
                widgets.Add(new("dora-metrics", "DORA Metrics", null, null, 0, row, 12, 5));
                row += 5;
            }

            // Persona-specific additions
            if (persona is "Executive" or "CTO")
            {
                widgets.Add(new("top-services", "Top Risk Services", null, null, 0, row, 6, 4));
                widgets.Add(new("team-health", "Team Health", null, null, 6, row, 6, 4));
            }

            return widgets;
        }

        private static string BuildGroundingContext(Command request)
        {
            var parts = new List<string>
            {
                $"persona:{request.Persona}",
                $"tenant:{request.TenantId}",
            };
            if (request.TeamId is not null) parts.Add($"team:{request.TeamId}");
            if (request.EnvironmentId is not null) parts.Add($"env:{request.EnvironmentId}");
            if (request.ServiceIds is { Count: > 0 })
                parts.Add($"services:{string.Join(",", request.ServiceIds.Take(5))}");
            return string.Join("|", parts);
        }
    }
}
