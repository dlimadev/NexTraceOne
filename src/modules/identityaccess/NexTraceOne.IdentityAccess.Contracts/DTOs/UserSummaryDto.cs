namespace NexTraceOne.IdentityAccess.Contracts.DTOs;

/// <summary>
/// DTO público com resumo do usuário para consumo por outros módulos.
/// </summary>
public sealed record UserSummaryDto(Guid UserId, string Email, string FullName, bool IsActive);
