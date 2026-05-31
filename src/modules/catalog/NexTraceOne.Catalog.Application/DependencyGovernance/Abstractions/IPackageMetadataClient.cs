namespace NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

/// <summary>
/// Cliente para consulta de metadados de pacotes em registries públicos.
/// </summary>
public interface IPackageMetadataClient
{
    /// <summary>
    /// Retorna a versão estável mais recente de um pacote.
    /// </summary>
    Task<string?> GetLatestStableVersionAsync(
        string packageName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna se o pacote está deprecado e a mensagem de depreciação.
    /// </summary>
    Task<(bool IsDeprecated, string? DeprecationMessage)> GetDeprecationInfoAsync(
        string packageName,
        string version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a licença do pacote.
    /// </summary>
    Task<string?> GetLicenseAsync(
        string packageName,
        string version,
        CancellationToken cancellationToken = default);
}
