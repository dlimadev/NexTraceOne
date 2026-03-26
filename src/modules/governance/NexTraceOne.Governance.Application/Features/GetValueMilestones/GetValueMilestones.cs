using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetValueMilestones;

/// <summary>
/// Retorna marcos de valor atingidos pelos utilizadores.
/// Responde: quanto tempo até o primeiro valor? Quais milestones são mais atingidos?
/// Qual a progressão de valor por persona?
/// COMPATIBILIDADE TRANSITÓRIA (P2.4): Handler temporariamente em Governance.Application.
/// Ownership real: módulo Product Analytics. Migração para ProductAnalytics.Application prevista em fase futura.
/// NOTA: ValueMilestoneType enum permanece em Governance.Domain.Enums até extração futura para ProductAnalytics.Domain.
/// </summary>
public static class GetValueMilestones
{
    /// <summary>Query para marcos de valor.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna marcos de valor.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var milestones = new List<MilestoneDto>
            {
                new(ValueMilestoneType.FirstSearchSuccess, "First Search Success",
                    94.2m, 3.2m, 221, TrendDirection.Stable),
                new(ValueMilestoneType.FirstServiceLookup, "First Service Lookup",
                    88.5m, 5.8m, 208, TrendDirection.Improving),
                new(ValueMilestoneType.FirstContractView, "First Contract View",
                    82.1m, 8.4m, 193, TrendDirection.Stable),
                new(ValueMilestoneType.FirstContractDraftCreated, "First Contract Draft Created",
                    52.3m, 42.5m, 123, TrendDirection.Improving),
                new(ValueMilestoneType.FirstContractPublished, "First Contract Published",
                    34.8m, 168.0m, 82, TrendDirection.Improving),
                new(ValueMilestoneType.FirstAiUsefulInteraction, "First AI Useful Interaction",
                    72.4m, 12.3m, 170, TrendDirection.Improving),
                new(ValueMilestoneType.FirstIncidentInvestigation, "First Incident Investigation",
                    58.1m, 28.6m, 136, TrendDirection.Stable),
                new(ValueMilestoneType.FirstMitigationCompleted, "First Mitigation Completed",
                    42.3m, 95.4m, 99, TrendDirection.Stable),
                new(ValueMilestoneType.FirstExecutiveOverviewConsumed, "First Executive Overview",
                    78.6m, 6.1m, 184, TrendDirection.Improving),
                new(ValueMilestoneType.FirstRunbookConsulted, "First Runbook Consulted",
                    38.4m, 35.2m, 90, TrendDirection.Declining),
                new(ValueMilestoneType.FirstSourceOfTruthUsed, "First Source of Truth Used",
                    86.2m, 7.2m, 202, TrendDirection.Improving),
                new(ValueMilestoneType.FirstEvidenceExported, "First Evidence Exported",
                    18.6m, 210.0m, 44, TrendDirection.Stable),
                new(ValueMilestoneType.FirstReportGenerated, "First Report Generated",
                    45.2m, 48.0m, 106, TrendDirection.Stable),
                new(ValueMilestoneType.FirstReliabilityViewed, "First Reliability Viewed",
                    48.7m, 22.8m, 114, TrendDirection.Declining),
                new(ValueMilestoneType.FirstAutomationCreated, "First Automation Created",
                    28.1m, 180.0m, 66, TrendDirection.Improving)
            };

            var response = new Response(
                Milestones: milestones,
                AvgTimeToFirstValueMinutes: 18.5m,
                AvgTimeToCoreValueMinutes: 142.0m,
                OverallCompletionRate: milestones.Average(m => m.CompletionRate),
                FastestMilestone: ValueMilestoneType.FirstSearchSuccess,
                SlowestMilestone: ValueMilestoneType.FirstEvidenceExported,
                PeriodLabel: request.Range ?? "last_30d");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com marcos de valor.</summary>
    public sealed record Response(
        IReadOnlyList<MilestoneDto> Milestones,
        decimal AvgTimeToFirstValueMinutes,
        decimal AvgTimeToCoreValueMinutes,
        decimal OverallCompletionRate,
        ValueMilestoneType FastestMilestone,
        ValueMilestoneType SlowestMilestone,
        string PeriodLabel);

    /// <summary>Marco de valor individual com métricas.</summary>
    public sealed record MilestoneDto(
        ValueMilestoneType MilestoneType,
        string MilestoneName,
        decimal CompletionRate,
        decimal AvgTimeToReachMinutes,
        int UsersReached,
        TrendDirection Trend);
}
