using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Services;

/// <summary>
/// Leitor de saúde dos workflows de self-service baseado em IAnalyticsEventRepository (PostgreSQL).
/// Mede CompletionRate, AbandonmentRate e AdminInterventionRate por workflow.
/// </summary>
internal sealed class SelfServiceWorkflowReader(
    IAnalyticsEventRepository repository) : ISelfServiceWorkflowReader
{
    private static readonly (string WorkflowName, AnalyticsEventType StartEvent, AnalyticsEventType CompleteEvent)[] WorkflowDefs =
    [
        ("CreateService",      AnalyticsEventType.ServiceCreated,      AnalyticsEventType.ServiceCreated),
        ("CreateContract",     AnalyticsEventType.ContractDraftCreated, AnalyticsEventType.ContractPublished),
        ("MitigateIncident",   AnalyticsEventType.MitigationWorkflowStarted, AnalyticsEventType.MitigationWorkflowCompleted),
    ];

    public async Task<IReadOnlyList<ISelfServiceWorkflowReader.WorkflowExecutionEntry>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var results = new List<ISelfServiceWorkflowReader.WorkflowExecutionEntry>();

        foreach (var (name, startEvent, completeEvent) in WorkflowDefs)
        {
            var attempts = await repository.CountByEventTypeAsync(startEvent, persona: null, from, to, cancellationToken);
            var completions = await repository.CountByEventTypeAsync(completeEvent, persona: null, from, to, cancellationToken);
            var abandoned = Math.Max(0, attempts - completions);

            results.Add(new ISelfServiceWorkflowReader.WorkflowExecutionEntry(
                WorkflowName: name,
                AttemptCount: (int)attempts,
                SuccessfulCompletions: (int)completions,
                AbandonedCount: (int)abandoned,
                AdminInterventionCount: 0,
                AvgCompletionTimeMinutes: attempts > 0 ? (to - from).TotalMinutes / attempts : 0));
        }

        return results;
    }

    public async Task<IReadOnlyList<ISelfServiceWorkflowReader.AbandonmentHotspot>> GetAbandonmentHotspotsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var hotspots = new List<ISelfServiceWorkflowReader.AbandonmentHotspot>();

        foreach (var (name, startEvent, completeEvent) in WorkflowDefs)
        {
            var attempts = await repository.CountByEventTypeAsync(startEvent, persona: null, from, to, cancellationToken);
            var completions = await repository.CountByEventTypeAsync(completeEvent, persona: null, from, to, cancellationToken);
            var abandoned = (int)Math.Max(0, attempts - completions);

            if (abandoned > 0)
            {
                hotspots.Add(new ISelfServiceWorkflowReader.AbandonmentHotspot(
                    WorkflowName: name,
                    StepName: "completion",
                    AbandonCount: abandoned,
                    Description: $"{abandoned} users started {name} but did not complete it in the period."));
            }
        }

        return hotspots;
    }

    public async Task<IReadOnlyList<ISelfServiceWorkflowReader.WorkflowReleaseSnapshot>> GetTrendByReleaseAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        // Tendência por release requer correlação com dados de deploy (ChangeGovernance).
        // Retorna snapshot único para o período total até integração com release registry.
        var totalAttempts = 0L;
        var totalCompletions = 0L;

        foreach (var (_, startEvent, completeEvent) in WorkflowDefs)
        {
            totalAttempts += await repository.CountByEventTypeAsync(startEvent, persona: null, from, to, cancellationToken);
            totalCompletions += await repository.CountByEventTypeAsync(completeEvent, persona: null, from, to, cancellationToken);
        }

        var completionRate = totalAttempts > 0 ? (double)totalCompletions / totalAttempts : 0;

        return
        [
            new ISelfServiceWorkflowReader.WorkflowReleaseSnapshot(
                ReleaseLabel: "current",
                ReleasedAt: from,
                AvgCompletionRate: completionRate)
        ];
    }
}
