using NexTraceOne.BuildingBlocks.Core.Events;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Domain.Events;

/// <summary>
/// Evento de domínio emitido quando um novo usuário é criado.
/// </summary>
public sealed record UserCreatedDomainEvent(UserId UserId, string Email) : DomainEventBase;
