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

namespace NexTraceOne.IdentityAccess.Application.Features.RevokeDelegation;

/// <summary>
/// Feature: RevokeDelegation — revoga uma delegação ativa antecipadamente.
/// Pode ser executado pelo delegante, pelo delegatário ou por um administrador.
/// </summary>
public static class RevokeDelegation
{
    /// <summary>Comando para revogação de delegação.</summary>
    public sealed record Command(Guid DelegationId) : ICommand;

    /// <summary>Valida a entrada da revogação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DelegationId).NotEmpty();
        }
    }

    /// <summary>Handler que processa a revogação de delegação.</summary>
    public sealed class Handler(
        IDelegationRepository delegationRepository,
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

            var delegation = await delegationRepository.GetByIdAsync(
                DelegationId.From(request.DelegationId), cancellationToken);

            if (delegation is null)
                return IdentityErrors.DelegationNotFound(request.DelegationId);

            var revokedBy = UserId.From(Guid.Parse(currentUser.Id));
            var now = dateTimeProvider.UtcNow;
            delegation.Revoke(revokedBy, now);

            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : delegation.TenantId;

            var securityEvent = SecurityEvent.Create(
                tenantId,
                delegation.DelegateeId,
                sessionId: null,
                SecurityEventType.DelegationRevoked,
                $"Delegation '{delegation.Id.Value}' revoked by '{revokedBy.Value}'.",
                riskScore: 55,
                ipAddress: null,
                userAgent: null,
                metadataJson: JsonSerializer.Serialize(new
                {
                    delegationId = delegation.Id.Value,
                    grantorId = delegation.GrantorId.Value,
                    delegateeId = delegation.DelegateeId.Value,
                    revokedBy = revokedBy.Value
                }),
                now);

            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return Unit.Value;
        }
    }
}
