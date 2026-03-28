using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Infrastructure.Persistence;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Providers;

/// <summary>
/// Implementação de IPlatformQueueMetricsProvider usando a tabela de outbox do módulo Governance.
/// Retorna contagens reais de mensagens pendentes e com falha da fila gov_outbox_messages.
/// Bounded context: apenas a fila de outbox deste módulo é acessível aqui.
/// </summary>
internal sealed class GovernanceOutboxQueueMetricsProvider(GovernanceDbContext context)
    : IPlatformQueueMetricsProvider
{
    private const int MaxRetryCount = 5;
    private const string QueueName = "gov_outbox_messages";
    private const string Subsystem = "Governance";

    public async Task<IReadOnlyList<QueueSnapshot>> GetQueueSnapshotsAsync(CancellationToken ct)
    {
        var messages = context.Set<OutboxMessage>();

        var pendingCount = await messages
            .CountAsync(m => m.ProcessedAt == null && m.RetryCount < MaxRetryCount, ct);

        var failedCount = await messages
            .CountAsync(m => m.ProcessedAt == null && m.RetryCount >= MaxRetryCount, ct);

        var lastActivityAt = await messages
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (DateTimeOffset?)m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return
        [
            new QueueSnapshot(
                QueueName: QueueName,
                Subsystem: Subsystem,
                PendingCount: pendingCount,
                FailedCount: failedCount,
                LastActivityAt: lastActivityAt)
        ];
    }
}

/// <summary>
/// Implementação de IPlatformJobStatusProvider que retorna o catálogo estático
/// de background jobs conhecidos da plataforma.
/// O estado de execução em runtime não está disponível no ApiHost — BackgroundWorkers
/// é um processo separado sem IPC direto. Todos os jobs são reportados como KnownJobSnapshot
/// sem dados de execução (honesto).
/// </summary>
internal sealed class KnownJobsStatusProvider : IPlatformJobStatusProvider
{
    private static readonly IReadOnlyList<KnownJobSnapshot> KnownJobs =
    [
        new("outbox-processor-governance",       "Outbox Processor — Governance",         "Processes gov_outbox_messages and dispatches integration events."),
        new("outbox-processor-identity",         "Outbox Processor — Identity",           "Processes identity_outbox_messages and dispatches integration events."),
        new("outbox-processor-catalog",          "Outbox Processor — Catalog",            "Processes catalog outbox queues for contract and service events."),
        new("outbox-processor-changegovernance", "Outbox Processor — Change Governance",  "Processes change governance outbox queues for workflow and approval events."),
        new("outbox-processor-ai",               "Outbox Processor — AI Knowledge",       "Processes ai_outbox_messages for knowledge capture and orchestration events."),
        new("identity-expiration",               "Identity Expiration Cleanup",           "Revokes expired sessions, delegations and Break Glass tokens."),
        new("drift-detection",                   "Contract Drift Detection",              "Detects semantic drift between deployed services and registered contracts.")
    ];

    public Task<IReadOnlyList<KnownJobSnapshot>> GetJobSnapshotsAsync(CancellationToken ct)
        => Task.FromResult(KnownJobs);
}

/// <summary>
/// Implementação de IPlatformEventProvider que deriva eventos operacionais
/// a partir de registos reais de rollout e waivers persistidos no GovernanceDbContext.
/// Mapeamento de severidade:
///   RolloutStatus.Failed      → Error,   Resolved = false
///   RolloutStatus.RolledBack  → Warning, Resolved = false
///   RolloutStatus.Completed   → Info,    Resolved = true
///   WaiverStatus.Rejected     → Warning, Resolved = true
///   WaiverStatus.Approved     → Info,    Resolved = true
///   WaiverStatus.Pending      → Warning, Resolved = false
/// </summary>
internal sealed class GovernanceEventProvider(GovernanceDbContext context) : IPlatformEventProvider
{
    public async Task<IReadOnlyList<GovernanceOperationalEvent>> GetRecentEventsAsync(
        int limit, CancellationToken ct)
    {
        var halfLimit = Math.Max(1, limit / 2);

        var rolloutEvents = await context.RolloutRecords
            .Where(r => r.Status == RolloutStatus.Completed
                     || r.Status == RolloutStatus.Failed
                     || r.Status == RolloutStatus.RolledBack)
            .OrderByDescending(r => r.CompletedAt ?? r.InitiatedAt)
            .Take(halfLimit)
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.Scope,
                Timestamp = r.CompletedAt ?? r.InitiatedAt
            })
            .ToListAsync(ct);

        var waiverEvents = await context.Waivers
            .Where(w => w.Status != WaiverStatus.Expired)
            .OrderByDescending(w => w.ReviewedAt ?? w.RequestedAt)
            .Take(halfLimit)
            .Select(w => new
            {
                w.Id,
                w.Status,
                w.Scope,
                Timestamp = w.ReviewedAt ?? w.RequestedAt
            })
            .ToListAsync(ct);

        var events = new List<GovernanceOperationalEvent>(rolloutEvents.Count + waiverEvents.Count);

        foreach (var r in rolloutEvents)
        {
            var (severity, resolved, message) = r.Status switch
            {
                RolloutStatus.Failed     => ("Error",   false, $"Governance pack rollout failed for scope '{r.Scope}'."),
                RolloutStatus.RolledBack => ("Warning", false, $"Governance pack was rolled back for scope '{r.Scope}'."),
                _                        => ("Info",    true,  $"Governance pack rollout completed for scope '{r.Scope}'.")
            };

            events.Add(new GovernanceOperationalEvent(
                EventId: $"rollout-{r.Id.Value}",
                Timestamp: r.Timestamp,
                Severity: severity,
                Subsystem: "Governance",
                Message: message,
                Resolved: resolved));
        }

        foreach (var w in waiverEvents)
        {
            var (severity, resolved, message) = w.Status switch
            {
                WaiverStatus.Rejected => ("Warning", true,  $"Governance waiver rejected for scope '{w.Scope}'."),
                WaiverStatus.Revoked  => ("Warning", true,  $"Governance waiver revoked for scope '{w.Scope}'."),
                WaiverStatus.Pending  => ("Warning", false, $"Governance waiver pending review for scope '{w.Scope}'."),
                _                     => ("Info",    true,  $"Governance waiver approved for scope '{w.Scope}'.")
            };

            events.Add(new GovernanceOperationalEvent(
                EventId: $"waiver-{w.Id.Value}",
                Timestamp: w.Timestamp,
                Severity: severity,
                Subsystem: "Governance",
                Message: message,
                Resolved: resolved));
        }

        return events
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList();
    }
}
