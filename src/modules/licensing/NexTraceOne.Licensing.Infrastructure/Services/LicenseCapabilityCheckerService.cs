using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Licensing.Contracts.ServiceInterfaces;

namespace NexTraceOne.Licensing.Infrastructure.Services;

/// <summary>
/// Implementação do verificador de capabilities para o pipeline MediatR.
/// Delega a verificação real ao <see cref="ILicensingModule"/>, evitando
/// duplicação de lógica de acesso a dados.
/// </summary>
internal sealed class LicenseCapabilityCheckerService(ILicensingModule licensingModule)
    : ILicenseCapabilityChecker
{
    public Task<bool> HasCapabilityAsync(
        string licenseKey,
        string capabilityCode,
        CancellationToken cancellationToken)
    {
        return licensingModule.HasCapabilityAsync(licenseKey, capabilityCode, cancellationToken);
    }
}
