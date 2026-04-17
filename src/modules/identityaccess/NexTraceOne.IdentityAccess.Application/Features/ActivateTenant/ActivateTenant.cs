using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.ActivateTenant;

/// <summary>
/// Feature: ActivateTenant — reactiva um tenant previamente desactivado.
/// Uso exclusivo de Platform Admin.
/// </summary>
public static class ActivateTenant
{
    /// <summary>Comando para reactivação de um tenant.</summary>
    public sealed record Command(Guid TenantId) : ICommand<Response>;

    /// <summary>Resposta com confirmação de reactivação.</summary>
    public sealed record Response(Guid TenantId, bool IsActive);

    /// <summary>
    /// Handler que reactiva o tenant.
    /// O commit é gerenciado pelo TransactionBehavior do pipeline.
    /// </summary>
    public sealed class Handler(
        ITenantRepository tenantRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tenant = await tenantRepository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken);
            if (tenant is null)
                return IdentityErrors.TenantNotFound(request.TenantId);

            if (tenant.IsActive)
                return Error.Validation("Identity.Tenant.AlreadyActive", "Tenant '{0}' is already active.", request.TenantId);

            tenant.Activate(dateTimeProvider.UtcNow);

            return Result<Response>.Success(new Response(tenant.Id.Value, tenant.IsActive));
        }
    }
}
