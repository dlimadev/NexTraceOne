using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeContractContext;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeServiceContext;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Services;

/// <summary>
/// Implementação null (honest-null) de IIdeContextReader.
/// Retorna null para ambos os métodos — sem bridge real configurado, IDE retorna NotFound.
/// Wave AK.1 — IDE Context API.
/// </summary>
public sealed class NullIdeContextReader : IIdeContextReader
{
    public Task<GetIdeServiceContext.ServiceContextSnapshot?> GetServiceContextAsync(
        string tenantId, string serviceName, CancellationToken cancellationToken = default)
        => Task.FromResult<GetIdeServiceContext.ServiceContextSnapshot?>(null);

    public Task<GetIdeContractContext.ContractContextSnapshot?> GetContractContextAsync(
        string tenantId, string contractName, CancellationToken cancellationToken = default)
        => Task.FromResult<GetIdeContractContext.ContractContextSnapshot?>(null);
}
