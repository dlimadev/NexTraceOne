using NexTraceOne.BuildingBlocks.Core.Events;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Domain.Events;

/// <summary>
/// Evento de domínio emitido quando um usuário é bloqueado por tentativas inválidas.
/// </summary>
public sealed record UserLockedDomainEvent(UserId UserId, DateTimeOffset LockoutEnd) : DomainEventBase;
