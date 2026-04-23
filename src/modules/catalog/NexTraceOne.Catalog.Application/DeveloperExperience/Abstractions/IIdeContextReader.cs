using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeContractContext;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeServiceContext;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

/// <summary>
/// Abstracção cross-module que fornece snapshots de contexto optimizados para extensões IDE.
/// Por omissão é satisfeita por <c>NullIdeContextReader</c> (honest-null pattern).
/// Wave AK.1 — IDE Context API.
/// </summary>
public interface IIdeContextReader
{
    Task<GetIdeServiceContext.ServiceContextSnapshot?> GetServiceContextAsync(
        string tenantId,
        string serviceName,
        CancellationToken cancellationToken = default);

    Task<GetIdeContractContext.ContractContextSnapshot?> GetContractContextAsync(
        string tenantId,
        string contractName,
        CancellationToken cancellationToken = default);
}
