using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Security;

/// <summary>
/// Registra: JWT authentication, TenantResolutionMiddleware, EncryptionService,
/// AssemblyIntegrityChecker, HardwareFingerprint.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: AddAuthentication, AddAuthorization, TenantMiddleware
        return services;
    }
}
