using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para ScheduledDashboardReport.</summary>
public sealed record ScheduledDashboardReportId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Configuração de relatório agendado para um CustomDashboard (V3.6).
/// Suporta snapshot periódico em PDF/PNG com entrega via SMTP ou webhook.
/// </summary>
public sealed class ScheduledDashboardReport : Entity<ScheduledDashboardReportId>
{
    /// <summary>Dashboard associado.</summary>
    public Guid DashboardId { get; private set; }

    /// <summary>Tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Utilizador que criou o agendamento.</summary>
    public string CreatedByUserId { get; private init; } = string.Empty;

    /// <summary>Expressão cron do agendamento (UTC). Ex: "0 9 * * 1" — todas as segundas às 09h UTC.</summary>
    public string CronExpression { get; private set; } = string.Empty;

    /// <summary>Formato do snapshot: "pdf" ou "png".</summary>
    public string Format { get; private set; } = "pdf";

    /// <summary>Lista de destinatários de email (JSON array de endereços).</summary>
    public string RecipientsJson { get; private set; } = "[]";

    /// <summary>URL de webhook para entrega alternativa ao email (nullable).</summary>
    public string? WebhookUrl { get; private set; }

    /// <summary>Dias de retenção dos snapshots gerados.</summary>
    public int RetentionDays { get; private set; } = 90;

    /// <summary>Indica se o agendamento está activo.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Data/hora UTC da última execução bem-sucedida.</summary>
    public DateTimeOffset? LastRunAt { get; private set; }

    /// <summary>Data/hora UTC da próxima execução calculada.</summary>
    public DateTimeOffset? NextRunAt { get; private set; }

    /// <summary>Número de execuções realizadas com sucesso.</summary>
    public int SuccessCount { get; private set; }

    /// <summary>Número de execuções com falha.</summary>
    public int FailureCount { get; private set; }

    /// <summary>Mensagem da última falha (nullable).</summary>
    public string? LastFailureMessage { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última modificação.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private ScheduledDashboardReport() { }

    public static ScheduledDashboardReport Create(
        Guid dashboardId,
        string tenantId,
        string userId,
        string cronExpression,
        string format,
        string recipientsJson,
        int retentionDays,
        DateTimeOffset now,
        string? webhookUrl = null)
    {
        Guard.Against.Default(dashboardId, nameof(dashboardId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));
        Guard.Against.NullOrWhiteSpace(format, nameof(format));
        Guard.Against.OutOfRange(retentionDays, nameof(retentionDays), 1, 3650);

        return new ScheduledDashboardReport
        {
            Id = new ScheduledDashboardReportId(Guid.NewGuid()),
            DashboardId = dashboardId,
            TenantId = tenantId,
            CreatedByUserId = userId,
            CronExpression = cronExpression.Trim(),
            Format = format.ToLowerInvariant(),
            RecipientsJson = recipientsJson,
            WebhookUrl = webhookUrl,
            RetentionDays = retentionDays,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RecordSuccess(DateTimeOffset runAt, DateTimeOffset? nextRunAt)
    {
        LastRunAt = runAt;
        NextRunAt = nextRunAt;
        SuccessCount++;
        LastFailureMessage = null;
        UpdatedAt = runAt;
    }

    public void RecordFailure(string message, DateTimeOffset now)
    {
        FailureCount++;
        LastFailureMessage = message;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    public void Update(string cronExpression, string format, string recipientsJson,
        int retentionDays, string? webhookUrl, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(cronExpression, nameof(cronExpression));
        CronExpression = cronExpression.Trim();
        Format = format.ToLowerInvariant();
        RecipientsJson = recipientsJson;
        RetentionDays = retentionDays;
        WebhookUrl = webhookUrl;
        UpdatedAt = now;
    }
}
