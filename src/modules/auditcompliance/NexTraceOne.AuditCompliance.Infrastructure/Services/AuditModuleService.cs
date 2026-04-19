using MediatR;

using NexTraceOne.AuditCompliance.Application.Features.RecordAuditEvent;
using NexTraceOne.AuditCompliance.Application.Features.VerifyChainIntegrity;
using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;

namespace NexTraceOne.AuditCompliance.Infrastructure.Services;

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
