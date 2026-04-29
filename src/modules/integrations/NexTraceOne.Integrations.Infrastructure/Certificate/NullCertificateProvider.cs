using Microsoft.Extensions.Configuration;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.Certificate;

/// <summary>
/// Implementação nula de ICertificateProvider.
/// Lê configuração básica de IConfiguration["Mtls:*"] mas não tem integração PKI real.
/// Retorna lista vazia de certificados enquanto cert-manager / Vault PKI não estiver configurado.
/// </summary>
internal sealed class NullCertificateProvider(IConfiguration configuration) : ICertificateProvider
{
    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(configuration["Mtls:CertManagerEndpoint"])
        || !string.IsNullOrWhiteSpace(configuration["Mtls:VaultPkiEndpoint"]);

    public Task<IReadOnlyList<CertificateInfo>> ListCertificatesAsync(
        string? tenantId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CertificateInfo>>([]);

    public Task<bool> RevokeCertificateAsync(
        string certId, string reason, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
