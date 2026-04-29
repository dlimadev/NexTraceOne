namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para integração com gestores de certificados PKI (cert-manager, Vault PKI, AWS ACM, …).
/// A implementação padrão é <c>NullCertificateProvider</c> que lê de IConfiguration.
/// DEG-05 — mTLS Certificate Manager.
/// </summary>
public interface ICertificateProvider
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<CertificateInfo>> ListCertificatesAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeCertificateAsync(
        string certId,
        string reason,
        CancellationToken cancellationToken = default);
}

/// <summary>Informação de um certificado gerido.</summary>
public sealed record CertificateInfo(
    string CertId,
    string Subject,
    string Issuer,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Status);
