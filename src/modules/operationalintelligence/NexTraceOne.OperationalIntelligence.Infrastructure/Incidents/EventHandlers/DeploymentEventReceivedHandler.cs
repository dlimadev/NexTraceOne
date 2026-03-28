using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.EventHandlers;

/// <summary>
/// Consome eventos de deployment do ChangeGovernance e cria um incidente de regressão
/// quando um deployment em produção é recebido, permitindo correlação automática change→incident.
/// </summary>
internal sealed class DeploymentEventReceivedHandler(
    IIncidentStore incidentStore,
    IIncidentCorrelationService incidentCorrelationService,
    ICurrentTenant currentTenant,
    ILogger<DeploymentEventReceivedHandler> logger) : IIntegrationEventHandler<DeploymentEventReceivedEvent>
{
    public async Task HandleAsync(DeploymentEventReceivedEvent @event, CancellationToken ct = default)
    {
        if (!string.Equals(@event.Environment, "production", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(@event.Environment, "prod", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(
                "Skipping deployment event {DeploymentId} in environment {Environment} for incident correlation seed",
                @event.DeploymentId,
                @event.Environment);
            return;
        }

        var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;
        var created = incidentStore.CreateIncident(new CreateIncidentInput(
            Title: $"Post-deploy verification triggered for {@event.SourceSystem}",
            Description: $"Deployment {@event.DeploymentId} version {@event.Version} received from {@event.SourceSystem}.",
            IncidentType: IncidentType.OperationalRegression,
            Severity: IncidentSeverity.Warning,
            ServiceId: @event.SourceSystem,
            ServiceDisplayName: @event.SourceSystem,
            OwnerTeam: "change-governance",
            ImpactedDomain: "change-intelligence",
            Environment: @event.Environment,
            DetectedAtUtc: @event.ReceivedAt,
            TenantId: tenantId,
            EnvironmentId: @event.EnvironmentId));

        _ = await incidentCorrelationService.RecomputeAsync(created.IncidentId.ToString(), ct);

        logger.LogInformation(
            "Incident {IncidentId} created from deployment event {DeploymentId}",
            created.IncidentId,
            @event.DeploymentId);
    }
}
