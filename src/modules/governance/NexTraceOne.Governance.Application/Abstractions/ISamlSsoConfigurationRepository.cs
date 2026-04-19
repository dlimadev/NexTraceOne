using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Contrato de repositório para configuração SAML SSO.</summary>
public interface ISamlSsoConfigurationRepository
{
    /// <summary>Obtém a configuração SAML activa para o tenant. Retorna null se não configurado.</summary>
    Task<SamlSsoConfiguration?> GetActiveAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>Adiciona uma nova configuração SAML.</summary>
    Task AddAsync(SamlSsoConfiguration config, CancellationToken ct);

    /// <summary>Atualiza uma configuração existente.</summary>
    void Update(SamlSsoConfiguration config);
}
