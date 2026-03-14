using NexTraceOne.BuildingBlocks.Core;

namespace NexTraceOne.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Evento de integração publicado quando o papel de um usuário muda em um tenant.
/// Consumidores: módulos que verificam permissões em cache (ex: AiOrchestration).
/// </summary>
public sealed record UserRoleChangedIntegrationEvent(Guid UserId, Guid TenantId, string RoleName)
    : IntegrationEventBase("Identity");
