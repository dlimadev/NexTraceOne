using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para SamlSsoConfiguration.</summary>
public sealed record SamlSsoConfigurationId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Configuração persistida do SAML SSO do tenant.
/// Substitui a leitura exclusiva de IConfiguration pelo armazenamento em base de dados.
/// </summary>
public sealed class SamlSsoConfiguration : Entity<SamlSsoConfigurationId>
{
    private SamlSsoConfiguration() { }

    /// <summary>EntityId do Service Provider SAML.</summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>URL do endpoint de Single Sign-On do Identity Provider.</summary>
    public string SsoUrl { get; private set; } = string.Empty;

    /// <summary>URL do endpoint de Single Log-Out do Identity Provider.</summary>
    public string SloUrl { get; private set; } = string.Empty;

    /// <summary>Certificado público do Identity Provider (PEM).</summary>
    public string IdpCertificate { get; private set; } = string.Empty;

    /// <summary>Indica se o provisionamento Just-In-Time está activo.</summary>
    public bool JitProvisioningEnabled { get; private set; }

    /// <summary>Role atribuída por defeito a utilizadores provisionados via JIT.</summary>
    public string DefaultRole { get; private set; } = "viewer";

    /// <summary>Mapeamentos de atributos SAML para claims da plataforma (JSON).</summary>
    public string AttributeMappingsJson { get; private set; } = "[]";

    /// <summary>Identificador do tenant.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Data da última actualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Cria ou actualiza a configuração SAML SSO.</summary>
    public static SamlSsoConfiguration Create(
        string entityId,
        string ssoUrl,
        string sloUrl,
        string idpCertificate,
        bool jitProvisioningEnabled,
        string defaultRole,
        string attributeMappingsJson,
        Guid? tenantId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(entityId);
        Guard.Against.NullOrWhiteSpace(ssoUrl);

        return new SamlSsoConfiguration
        {
            Id = new SamlSsoConfigurationId(Guid.NewGuid()),
            EntityId = entityId.Trim(),
            SsoUrl = ssoUrl.Trim(),
            SloUrl = sloUrl?.Trim() ?? string.Empty,
            IdpCertificate = idpCertificate?.Trim() ?? string.Empty,
            JitProvisioningEnabled = jitProvisioningEnabled,
            DefaultRole = defaultRole?.Trim() ?? "viewer",
            AttributeMappingsJson = attributeMappingsJson ?? "[]",
            TenantId = tenantId,
            UpdatedAt = now
        };
    }

    /// <summary>Actualiza a configuração SAML.</summary>
    public void Update(
        string entityId,
        string ssoUrl,
        string sloUrl,
        string idpCertificate,
        bool jitProvisioningEnabled,
        string defaultRole,
        string attributeMappingsJson,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(entityId);
        Guard.Against.NullOrWhiteSpace(ssoUrl);

        EntityId = entityId.Trim();
        SsoUrl = ssoUrl.Trim();
        SloUrl = sloUrl?.Trim() ?? string.Empty;
        IdpCertificate = idpCertificate?.Trim() ?? string.Empty;
        JitProvisioningEnabled = jitProvisioningEnabled;
        DefaultRole = defaultRole?.Trim() ?? "viewer";
        AttributeMappingsJson = attributeMappingsJson ?? "[]";
        UpdatedAt = now;
    }
}
