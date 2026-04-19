using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;
using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetAdoptionFunnel;

/// <summary>
/// Retorna funil de adoção por módulo.
/// Responde: onde os utilizadores abandonam fluxos críticos por módulo?
/// Cada módulo tem um funil padrão que mede progressão de engagement.
/// Cohort-based: métricas baseadas em sessões reais no período.
/// </summary>
public static class GetAdoptionFunnel
{
    /// <summary>Query para funil de adoção por módulo com paginação.</summary>
    public sealed record Query(
        string? Module,
        string? Persona,
        string? TeamId,
        string? Range,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que calcula funis de adoção por módulo a partir de dados reais.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        private static readonly ModuleFunnelDef[] FunnelDefinitions =
        [
            new(ProductModule.ContractStudio, "Contract Studio",
            [
                new("module_entered", "Module Entered", AnalyticsEventType.ModuleViewed),
                new("draft_created", "Draft Created", AnalyticsEventType.ContractDraftCreated),
                new("contract_published", "Contract Published", AnalyticsEventType.ContractPublished)
            ]),
            new(ProductModule.ChangeIntelligence, "Change Intelligence",
            [
                new("module_entered", "Module Entered", AnalyticsEventType.ModuleViewed),
                new("change_viewed", "Change Viewed", AnalyticsEventType.ChangeViewed),
                new("incident_investigated", "Incident Investigated", AnalyticsEventType.IncidentInvestigated)
            ]),
            new(ProductModule.Incidents, "Incidents & Mitigation",
            [
                new("module_entered", "Module Entered", AnalyticsEventType.ModuleViewed),
                new("incident_investigated", "Incident Investigated", AnalyticsEventType.IncidentInvestigated),
                new("mitigation_started", "Mitigation Started", AnalyticsEventType.MitigationWorkflowStarted),
                new("mitigation_completed", "Mitigation Completed", AnalyticsEventType.MitigationWorkflowCompleted)
            ]),
            new(ProductModule.AiAssistant, "AI Assistant",
            [
                new("module_entered", "Module Entered", AnalyticsEventType.ModuleViewed),
                new("prompt_submitted", "Prompt Submitted", AnalyticsEventType.AssistantPromptSubmitted),
                new("response_used", "Response Used", AnalyticsEventType.AssistantResponseUsed)
            ]),
            new(ProductModule.SourceOfTruth, "Source of Truth",
            [
                new("module_entered", "Module Entered", AnalyticsEventType.ModuleViewed),
                new("search_executed", "Search Executed", AnalyticsEventType.SearchExecuted),
                new("entity_viewed", "Entity Viewed", AnalyticsEventType.EntityViewed)
            ]),
            new(ProductModule.Governance, "Governance",
            [
                new("module_entered", "Module Entered", AnalyticsEventType.ModuleViewed),
                new("policy_viewed", "Policy Viewed", AnalyticsEventType.PolicyViewed),
                new("report_generated", "Report Generated", AnalyticsEventType.ReportGenerated)
            ])
        ];

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxRangeCfg = await configService.ResolveEffectiveValueAsync(AnalyticsConfigKeys.MaxRangeDays, ConfigurationScope.System, null, cancellationToken);
            var maxRangeDays = int.TryParse(maxRangeCfg?.EffectiveValue, out var mrd) ? mrd : AnalyticsConstants.MaxRangeDays;

            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range, maxRangeDays);

            var definitions = FunnelDefinitions.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Module) &&
                Enum.TryParse<ProductModule>(request.Module, true, out var moduleFilter))
            {
                definitions = definitions.Where(d => d.Module == moduleFilter);
            }

            var allEventTypes = definitions
                .SelectMany(d => d.Steps.Select(s => s.EventType))
                .Distinct()
                .ToArray();

            var sessionEvents = await repository.GetSessionEventTypesAsync(
                allEventTypes, request.Persona, from, to, cancellationToken);

            var sessionMap = sessionEvents
                .GroupBy(e => e.SessionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.EventType).ToHashSet());

            var funnels = new List<ModuleFunnelDto>();

            foreach (var def in definitions)
            {
                var firstStepType = def.Steps[0].EventType;
                var sessionsWithEntry = sessionMap
                    .Where(s => s.Value.Contains(firstStepType))
                    .ToList();

                var totalSessions = sessionsWithEntry.Count;

                var steps = new List<FunnelStepDto>();
                var biggestDropOff = string.Empty;
                var biggestDropValue = 0m;

                for (var i = 0; i < def.Steps.Length; i++)
                {
                    var step = def.Steps[i];
                    var reached = totalSessions == 0
                        ? 0
                        : sessionsWithEntry.Count(s => s.Value.Contains(step.EventType));

                    var percent = totalSessions > 0
                        ? Math.Round((reached / (decimal)totalSessions) * 100m, 1)
                        : 0m;

                    steps.Add(new FunnelStepDto(step.StepId, step.StepName, reached, percent));

                    if (i > 0)
                    {
                        var drop = steps[i - 1].CompletionPercent - percent;
                        if (drop > biggestDropValue)
                        {
                            biggestDropValue = drop;
                            biggestDropOff = $"{def.Steps[i - 1].StepName} → {step.StepName}";
                        }
                    }
                }

                var completionRate = steps.Count > 0 ? steps[^1].CompletionPercent : 0m;

                funnels.Add(new ModuleFunnelDto(
                    def.Module, def.ModuleName, steps, completionRate,
                    totalSessions, biggestDropOff));
            }

            var totalCount = funnels.Count;
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            var pagedFunnels = funnels.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Result<Response>.Success(new Response(
                Funnels: pagedFunnels,
                PeriodLabel: periodLabel,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: Math.Max(1, totalPages)));
        }

        private static (DateTimeOffset From, DateTimeOffset To, string Label) ResolveRange(DateTimeOffset utcNow, string? range, int maxDays = AnalyticsConstants.MaxRangeDays)
        {
            var label = string.IsNullOrWhiteSpace(range) ? "last_30d" : range;
            var days = label switch
            {
                "last_7d" => 7,
                "last_1d" => 1,
                "last_90d" => 90,
                _ => 30
            };
            if (days > maxDays) days = maxDays;
            return (utcNow.AddDays(-days), utcNow, label);
        }
    }

    private sealed record ModuleFunnelDef(ProductModule Module, string ModuleName, FunnelStepDef[] Steps);
    private sealed record FunnelStepDef(string StepId, string StepName, AnalyticsEventType EventType);

    /// <summary>Resposta com funis de adoção por módulo e metadados de paginação.</summary>
    public sealed record Response(
        IReadOnlyList<ModuleFunnelDto> Funnels,
        string PeriodLabel,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    /// <summary>Funil de adoção de um módulo individual.</summary>
    public sealed record ModuleFunnelDto(
        ProductModule Module,
        string ModuleName,
        IReadOnlyList<FunnelStepDto> Steps,
        decimal CompletionRate,
        int TotalSessions,
        string BiggestDropOff);

    /// <summary>Step de funil com contagem de sessões e taxa de conclusão.</summary>
    public sealed record FunnelStepDto(
        string StepId,
        string StepName,
        int SessionCount,
        decimal CompletionPercent);
}
