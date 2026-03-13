using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Domain.Events;

/// <summary>
/// Evento de domínio emitido quando um usuário é bloqueado por tentativas inválidas.
/// </summary>
public sealed record UserLockedDomainEvent(UserId UserId, DateTimeOffset LockoutEnd) : DomainEventBase;
