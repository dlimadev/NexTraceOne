using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Contracts;

/// <summary>
/// DTO de resposta do contexto de execução resolvido.
/// Retornado ao frontend para que ele possa materializar experiência contextual
/// sem tomar decisões de segurança de forma autônoma.
///
/// O backend é a fonte de verdade — este DTO reflete o estado validado.
/// O frontend usa este DTO para:
/// - Exibir badge de ambiente com cor e nome corretos
/// - Mostrar avisos de proteção em ambientes críticos
/// - Habilitar/desabilitar funcionalidades por contexto
/// - Configurar o scope padrão de filtros e visualizações
/// </summary>
public sealed record RuntimeContextDto(
    /// <summary>Contexto do usuário autenticado.</summary>
    RuntimeUserDto User,

    /// <summary>Contexto do tenant ativo.</summary>
    RuntimeTenantDto Tenant,

    /// <summary>Contexto do ambiente ativo, ou null se não houver ambiente resolvido.</summary>
    RuntimeEnvironmentDto? Environment,

    /// <summary>Indica se o contexto operacional completo está disponível (tenant + ambiente + usuário).</summary>
    bool IsFullyResolved,

    /// <summary>Timestamp UTC da resolução do contexto.</summary>
    DateTimeOffset ResolvedAt);

/// <summary>Contexto de usuário para o frontend.</summary>
public sealed record RuntimeUserDto(
    string UserId,
    string UserName,
    string Email,
    bool IsAuthenticated);

/// <summary>Contexto de tenant para o frontend.</summary>
public sealed record RuntimeTenantDto(
    Guid TenantId,
    string TenantSlug,
    string TenantName,
    bool IsActive);

/// <summary>
/// Contexto de ambiente para o frontend.
/// Inclui informações de comportamento UI geradas pelo backend.
/// </summary>
public sealed record RuntimeEnvironmentDto(
    Guid EnvironmentId,
    EnvironmentProfile Profile,
    string ProfileDisplayName,
    bool IsProductionLike,
    string BadgeColor,
    bool ShowProtectionWarning,
    bool AllowDestructiveActions);
