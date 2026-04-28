using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Registo de um alerta que disparou (ou resolveu) num dado momento.
/// Gerado pelo AlertEvaluationJob a partir de UserAlertRule.
/// Prefixo de tabela: cfg_ (usa ConfigurationDbContext cross-module read)
/// Prefixo de tabela aqui: iam_ (owned by IdentityAccess para simplicidade)
/// </summary>
public sealed class AlertFiringRecord : Entity<AlertFiringRecordId>
{
    private AlertFiringRecord() { }

    public Guid TenantId { get; private set; }
    public Guid AlertRuleId { get; private set; }
    public string AlertRuleName { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public string ConditionSummary { get; private set; } = string.Empty;
    public string? ServiceName { get; private set; }
    public AlertFiringStatus Status { get; private set; }
    public DateTimeOffset FiredAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolvedReason { get; private set; }
    public string? NotificationChannels { get; private set; }

    public static AlertFiringRecord Fire(
        Guid tenantId,
        Guid alertRuleId,
        string alertRuleName,
        string severity,
        string conditionSummary,
        string? serviceName,
        string? notificationChannels,
        DateTimeOffset now)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Default(alertRuleId);
        Guard.Against.NullOrWhiteSpace(alertRuleName);

        return new AlertFiringRecord
        {
            Id = AlertFiringRecordId.New(),
            TenantId = tenantId,
            AlertRuleId = alertRuleId,
            AlertRuleName = alertRuleName.Trim(),
            Severity = string.IsNullOrWhiteSpace(severity) ? "Medium" : severity.Trim(),
            ConditionSummary = conditionSummary?.Trim() ?? string.Empty,
            ServiceName = serviceName?.Trim(),
            Status = AlertFiringStatus.Firing,
            FiredAt = now,
            NotificationChannels = notificationChannels,
        };
    }

    public void Resolve(string? reason, DateTimeOffset now)
    {
        Status = AlertFiringStatus.Resolved;
        ResolvedAt = now;
        ResolvedReason = reason?.Trim();
    }

    public void Silence(DateTimeOffset now)
    {
        Status = AlertFiringStatus.Silenced;
        ResolvedAt = now;
    }
}
