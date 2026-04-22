namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstração de leitura de metadados de segurança de serviços para avaliação de postura Zero Trust.
///
/// Fornece dados por serviço cobrindo as quatro dimensões de Zero Trust:
/// autenticação, mTLS, rotação de tokens e cobertura de políticas de acesso.
/// Desacopla o handler de zero trust das implementações concretas de repositório.
///
/// Wave AD.1 — GetZeroTrustPostureReport.
/// </summary>
public interface IZeroTrustServiceReader
{
    /// <summary>
    /// Lista todas as entradas de segurança de serviços para um tenant.
    /// </summary>
    Task<IReadOnlyList<ServiceSecurityEntry>> ListByTenantAsync(string tenantId, CancellationToken ct);
}

/// <summary>
/// Entrada de metadados de segurança de um serviço para avaliação de postura Zero Trust.
/// Wave AD.1.
/// </summary>
public sealed record ServiceSecurityEntry(
    /// <summary>Identificador único do serviço.</summary>
    string ServiceId,
    /// <summary>Nome do serviço.</summary>
    string ServiceName,
    /// <summary>Nome da equipa proprietária, ou null se não atribuído.</summary>
    string? TeamName,
    /// <summary>Tier do serviço: Critical, Standard ou Experimental.</summary>
    string ServiceTier,
    /// <summary>Indica se o serviço tem esquema de autenticação definido (Bearer/mTLS/API Key).</summary>
    bool HasAuthenticationScheme,
    /// <summary>Indica se o mTLS está habilitado para comunicação inter-serviço.</summary>
    bool MtlsEnabled,
    /// <summary>Indica se existe política de rotação de tokens definida.</summary>
    bool HasTokenRotationPolicy,
    /// <summary>Número de PolicyDefinitions de acesso aplicadas ao serviço.</summary>
    int PolicyDefinitionCount);
