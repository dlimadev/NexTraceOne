namespace NexTraceOne.Identity.Contracts.DTOs;

/// <summary>
/// DTO público com dados de vínculo de tenant de um usuário.
/// </summary>
public sealed record TenantMembershipDto(Guid TenantId, Guid RoleId, string RoleName, bool IsActive);
