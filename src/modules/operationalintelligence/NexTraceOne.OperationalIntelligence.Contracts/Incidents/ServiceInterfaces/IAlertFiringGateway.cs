namespace NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

/// <summary>
/// Contrato cross-module para registo de alertas operacionais disparados.
/// Permite que módulos externos (ex.: IdentityAccess.AlertEvaluationJob) gravem alertas
/// no OperationalIntelligence sem referência directa ao seu DbContext ou repositórios internos.
/// </summary>
public interface IAlertFiringGateway
{
    /// <summary>Verifica se já existe um alerta em estado Firing para a regra indicada.</summary>
    Task<bool> HasFiringAlertAsync(Guid tenantId, Guid alertRuleId, CancellationToken ct = default);

    /// <summary>Grava um novo registo de alerta disparado.</summary>
    Task RecordFiringAlertAsync(
        Guid tenantId,
        Guid alertRuleId,
        string alertRuleName,
        string severity,
        string conditionSummary,
        string? serviceName,
        string? notificationChannels,
        DateTimeOffset firedAt,
        CancellationToken ct = default);
}
