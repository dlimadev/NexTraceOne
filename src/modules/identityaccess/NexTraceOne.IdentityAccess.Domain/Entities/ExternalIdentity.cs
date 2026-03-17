using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que mapeia uma identidade externa (OIDC, SAML, SCIM) para um usuário interno.
/// Permite que um usuário tenha múltiplas identidades federadas vinculadas (e.g., Azure AD + Okta).
/// Armazena os grupos/claims do provedor para mapeamento automático para roles internas.
/// </summary>
public sealed class ExternalIdentity : Entity<ExternalIdentityId>
{
    private ExternalIdentity() { }

    /// <summary>Usuário interno vinculado a esta identidade externa.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Nome do provedor federado (e.g., "AzureAD", "Okta", "Keycloak").</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Identificador único do usuário no provedor externo (subject/nameId).</summary>
    public string ExternalUserId { get; private set; } = string.Empty;

    /// <summary>Email retornado pelo provedor externo, para correlação e auditoria.</summary>
    public string? ExternalEmail { get; private set; }

    /// <summary>
    /// Grupos/claims serializados do provedor externo em formato JSON.
    /// Usado para mapeamento automático de roles internas via SsoGroupMapping.
    /// </summary>
    public string? ExternalGroupsJson { get; private set; }

    /// <summary>Data/hora UTC da última sincronização de dados do provedor.</summary>
    public DateTimeOffset LastSyncAt { get; private set; }

    /// <summary>Data/hora UTC em que o vínculo foi criado.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Cria um vínculo de identidade externa para um usuário interno.</summary>
    public static ExternalIdentity Create(
        UserId userId,
        string provider,
        string externalUserId,
        string? externalEmail,
        DateTimeOffset now)
    {
        Guard.Against.Null(userId);
        Guard.Against.NullOrWhiteSpace(provider);
        Guard.Against.NullOrWhiteSpace(externalUserId);

        return new ExternalIdentity
        {
            Id = ExternalIdentityId.New(),
            UserId = userId,
            Provider = provider,
            ExternalUserId = externalUserId,
            ExternalEmail = externalEmail,
            LastSyncAt = now,
            CreatedAt = now
        };
    }

    /// <summary>Atualiza os grupos/claims sincronizados do provedor externo.</summary>
    public void UpdateExternalGroups(string? groupsJson, DateTimeOffset now)
    {
        ExternalGroupsJson = groupsJson;
        LastSyncAt = now;
    }

    /// <summary>Atualiza o email externo quando o provedor retorna um valor diferente.</summary>
    public void UpdateExternalEmail(string? email, DateTimeOffset now)
    {
        ExternalEmail = email;
        LastSyncAt = now;
    }
}

/// <summary>Identificador fortemente tipado de ExternalIdentity.</summary>
public sealed record ExternalIdentityId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalIdentityId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalIdentityId From(Guid id) => new(id);
}
