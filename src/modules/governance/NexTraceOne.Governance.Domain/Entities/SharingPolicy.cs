using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Política de partilha granular de um CustomDashboard (V3.1 — Dashboard Intelligence Foundation).
/// Substitui o bool IsShared com controlo fino de âmbito e permissões.
/// Serializado como JSONB em CustomDashboard.SharingPolicyJson.
/// </summary>
public sealed record SharingPolicy(
    DashboardSharingScope Scope,
    DashboardSharingPermission Permission,
    DateTimeOffset? SignedLinkExpiresAt = null)
{
    /// <summary>Política padrão: privado, apenas leitura.</summary>
    public static SharingPolicy Private => new(DashboardSharingScope.Private, DashboardSharingPermission.Read);

    /// <summary>Retrocompat: converte IsShared=true para Tenant/Read.</summary>
    public static SharingPolicy FromLegacyIsShared(bool isShared)
        => isShared
            ? new SharingPolicy(DashboardSharingScope.Tenant, DashboardSharingPermission.Read)
            : Private;

    /// <summary>Indica se o dashboard é visível fora do criador.</summary>
    public bool IsVisible => Scope != DashboardSharingScope.Private;

    /// <summary>Indica se o link público está ativo e não expirou.</summary>
    public bool HasActivePublicLink(DateTimeOffset now)
        => Scope == DashboardSharingScope.PublicLink
           && (SignedLinkExpiresAt is null || SignedLinkExpiresAt > now);
}
