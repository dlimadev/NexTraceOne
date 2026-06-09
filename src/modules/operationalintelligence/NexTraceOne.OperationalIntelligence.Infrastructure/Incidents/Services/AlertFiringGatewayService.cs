using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Services;

/// <summary>
/// Implementação cross-module do gateway de alertas operacionais.
/// Permite que módulos externos gravem AlertFiringRecord no contexto OI
/// sem referência directa ao IncidentResponseDbContext.
/// </summary>
internal sealed class AlertFiringGatewayService(
    IAlertFiringRecordRepository repository,
    IIncidentResponseUnitOfWork unitOfWork) : IAlertFiringGateway
{
    public Task<bool> HasFiringAlertAsync(Guid tenantId, Guid alertRuleId, CancellationToken ct = default)
        => repository.HasFiringAlertAsync(tenantId, alertRuleId, ct);

    public async Task RecordFiringAlertAsync(
        Guid tenantId,
        Guid alertRuleId,
        string alertRuleName,
        string severity,
        string conditionSummary,
        string? serviceName,
        string? notificationChannels,
        DateTimeOffset firedAt,
        CancellationToken ct = default)
    {
        var record = AlertFiringRecord.Fire(tenantId, alertRuleId, alertRuleName, severity,
            conditionSummary, serviceName, notificationChannels, firedAt);

        repository.Add(record);
        await unitOfWork.CommitAsync(ct);
    }
}
