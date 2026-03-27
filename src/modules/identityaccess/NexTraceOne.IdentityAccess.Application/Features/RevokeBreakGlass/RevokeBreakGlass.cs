using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;
using System.Text.Json;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.RevokeBreakGlass;

/// <summary>
/// Feature: RevokeBreakGlass — revoga manualmente um acesso emergencial ativo.
/// Apenas administradores ou o próprio solicitante podem revogar.
/// </summary>
public static class RevokeBreakGlass
{
    /// <summary>Comando para revogação manual de acesso emergencial.</summary>
    public sealed record Command(Guid RequestId) : ICommand;

    /// <summary>Valida a entrada da revogação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    /// <summary>Handler que processa a revogação de acesso emergencial.</summary>
    public sealed class Handler(
        IBreakGlassRepository breakGlassRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var breakGlass = await breakGlassRepository.GetByIdAsync(
                BreakGlassRequestId.From(request.RequestId), cancellationToken);

            if (breakGlass is null)
                return IdentityErrors.BreakGlassNotFound(request.RequestId);

            if (!breakGlass.IsActiveAt(dateTimeProvider.UtcNow))
                return IdentityErrors.BreakGlassNotActive(request.RequestId);

            var revokedBy = UserId.From(Guid.Parse(currentUser.Id));
            var now = dateTimeProvider.UtcNow;
            breakGlass.Revoke(revokedBy, now);

            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : breakGlass.TenantId;

            var securityEvent = SecurityEvent.Create(
                tenantId,
                breakGlass.RequestedBy,
                sessionId: null,
                SecurityEventType.BreakGlassRevoked,
                $"Break glass request '{breakGlass.Id.Value}' revoked by '{revokedBy.Value}'.",
                riskScore: 70,
                ipAddress: null,
                userAgent: null,
                metadataJson: JsonSerializer.Serialize(new
                {
                    breakGlassRequestId = breakGlass.Id.Value,
                    requestedBy = breakGlass.RequestedBy.Value,
                    revokedBy = revokedBy.Value
                }),
                now);

            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return Unit.Value;
        }
    }
}
