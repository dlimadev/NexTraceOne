using MediatR;

using NexTraceOne.Governance.Application.AuditCompliance.Features.RecordAuditEvent;
using NexTraceOne.Governance.Application.AuditCompliance.Features.VerifyChainIntegrity;
using NexTraceOne.Governance.Contracts.AuditCompliance.ServiceInterfaces;

namespace NexTraceOne.Governance.Infrastructure.AuditCompliance.Services;

internal sealed class AuditModuleService(ISender sender) : IAuditModule
{
    public async Task RecordEventAsync(
        string sourceModule, string actionType, string resourceId, string resourceType,
        string performedBy, Guid tenantId, string? payload, CancellationToken cancellationToken,
        string? correlationId = null)
    {
        await sender.Send(new RecordAuditEvent.Command(sourceModule, actionType, resourceId, resourceType, performedBy, tenantId, payload, correlationId), cancellationToken);
    }

    public async Task<bool> VerifyChainIntegrityAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyChainIntegrity.Query(), cancellationToken);
        return result.IsSuccess && result.Value.IsIntact;
    }
}
