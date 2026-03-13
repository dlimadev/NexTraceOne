namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para verificação de capabilities da licença.
/// Implementada pelo módulo Licensing e consumida pelo
/// LicenseCapabilityBehavior no pipeline MediatR.
///
/// Design: esta interface vive no BuildingBlocks.Application para
/// evitar dependência circular entre módulos e building blocks.
/// O módulo Licensing registra sua implementação no DI container.
/// Se o módulo Licensing não estiver registrado, o behavior
/// permite a execução (fail-open para módulos opcionais).
/// </summary>
public interface ILicenseCapabilityChecker
{
    /// <summary>
    /// Verifica se a licença possui a capability especificada habilitada.
    /// </summary>
    /// <param name="licenseKey">Chave pública da licença.</param>
    /// <param name="capabilityCode">Código da capability a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a capability está habilitada; <c>false</c> caso contrário.</returns>
    Task<bool> HasCapabilityAsync(string licenseKey, string capabilityCode, CancellationToken cancellationToken);
}
