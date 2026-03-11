using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Domain.Events;

/// <summary>
/// Evento de domínio emitido quando um novo usuário é criado.
/// </summary>
public sealed record UserCreatedDomainEvent(UserId UserId, string Email) : DomainEventBase;
