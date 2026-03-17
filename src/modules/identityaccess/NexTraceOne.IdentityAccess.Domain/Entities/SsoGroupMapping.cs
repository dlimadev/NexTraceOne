using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que mapeia um grupo externo do SSO (OIDC/SAML) para um papel interno da plataforma.
/// Quando um usuário autentica via SSO, seus grupos são comparados contra estes mapeamentos
/// para atribuir automaticamente o role adequado no tenant correspondente.
///
/// Regras:
/// - Um grupo externo pode mapear para um único role interno por tenant.
/// - Cada mapeamento é por provedor + tenant, evitando acoplamento direto com um SSO específico.
/// - A sincronização é auditada e pode ser ativada/desativada individualmente.
/// </summary>
public sealed class SsoGroupMapping : Entity<SsoGroupMappingId>
{
    private SsoGroupMapping() { }

    /// <summary>Tenant ao qual este mapeamento se aplica.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Nome do provedor federado (e.g., "AzureAD", "Okta", "Keycloak").</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Identificador ou nome do grupo externo no provedor SSO.
    /// Para Azure AD, normalmente é o Object ID do grupo; para Keycloak, pode ser o nome.
    /// </summary>
    public string ExternalGroupId { get; private set; } = string.Empty;

    /// <summary>Nome legível do grupo externo, para exibição na UI de administração.</summary>
    public string ExternalGroupName { get; private set; } = string.Empty;

    /// <summary>Role interno atribuído quando o grupo externo é detectado.</summary>
    public RoleId RoleId { get; private set; } = null!;

    /// <summary>Indica se o mapeamento está ativo. Mapeamentos desativados são ignorados na sincronização.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação do mapeamento.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Cria um novo mapeamento de grupo SSO para role interno.</summary>
    public static SsoGroupMapping Create(
        TenantId tenantId,
        string provider,
        string externalGroupId,
        string externalGroupName,
        RoleId roleId,
        DateTimeOffset now)
    {
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(provider);
        Guard.Against.NullOrWhiteSpace(externalGroupId);
        Guard.Against.NullOrWhiteSpace(externalGroupName);
        Guard.Against.Null(roleId);

        return new SsoGroupMapping
        {
            Id = SsoGroupMappingId.New(),
            TenantId = tenantId,
            Provider = provider,
            ExternalGroupId = externalGroupId,
            ExternalGroupName = externalGroupName,
            RoleId = roleId,
            IsActive = true,
            CreatedAt = now
        };
    }

    /// <summary>Altera o role interno associado a este mapeamento.</summary>
    public void ChangeRole(RoleId roleId)
        => RoleId = Guard.Against.Null(roleId);

    /// <summary>Desativa o mapeamento para que seja ignorado na próxima sincronização.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa um mapeamento previamente desativado.</summary>
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de SsoGroupMapping.</summary>
public sealed record SsoGroupMappingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SsoGroupMappingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SsoGroupMappingId From(Guid id) => new(id);
}
