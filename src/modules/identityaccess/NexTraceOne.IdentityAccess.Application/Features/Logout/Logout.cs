using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.Logout;

/// <summary>
/// Feature: Logout — revoga a sessão ativa do usuário autenticado.
/// Gera evento de segurança para trilha de auditoria.
/// </summary>
public static class Logout
{
    /// <summary>Comando de logout que revoga a sessão ativa do usuário atual.</summary>
    public sealed record Command : ICommand;

    /// <summary>Handler que revoga a sessão ativa e registra evento de segurança.</summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ISessionRepository sessionRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.Id, out var userId))
            {
                return IdentityErrors.NotAuthenticated();
            }

            var session = await sessionRepository.GetActiveByUserIdAsync(
                UserId.From(userId),
                cancellationToken);

            if (session is not null)
            {
                session.Revoke(dateTimeProvider.UtcNow);
            }

            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : TenantId.From(Guid.Empty);

            var securityEvent = SecurityEvent.Create(
                tenantId,
                UserId.From(userId),
                session?.Id,
                SecurityEventType.LogoutPerformed,
                $"User '{userId}' logged out.",
                riskScore: 0,
                ipAddress: null,
                userAgent: null,
                metadataJson: null,
                dateTimeProvider.UtcNow);
            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return Unit.Value;
        }
    }
}
