using MediatR;
using NexTraceOne.Audit.Application.Features.RecordAuditEvent;
using NexTraceOne.Audit.Application.Features.VerifyChainIntegrity;
using NexTraceOne.Audit.Contracts.ServiceInterfaces;

namespace NexTraceOne.Audit.Infrastructure.Services;

internal sealed class AuditModuleService(ISender sender) : IAuditModule
{
    public async Task RecordEventAsync(
        string sourceModule, string actionType, string resourceId, string resourceType,
        string performedBy, Guid tenantId, string? payload, CancellationToken cancellationToken)
    {
        await sender.Send(new RecordAuditEvent.Command(sourceModule, actionType, resourceId, resourceType, performedBy, tenantId, payload), cancellationToken);
    }

    public async Task<bool> VerifyChainIntegrityAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyChainIntegrity.Query(), cancellationToken);
        return result.IsSuccess && result.Value.IsIntact;
    }
}
