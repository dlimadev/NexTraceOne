using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AuditCompliance.Application;
using NexTraceOne.AuditCompliance.Infrastructure;
using NexTraceOne.Governance.Application;
using NexTraceOne.Governance.Infrastructure;

namespace NexTraceOne.Governance.API;

/// <summary>
/// Registra serviços do bounded context PlatformGovernance.
/// Unifica Policy &amp; Risk Governance (gov_) e Audit &amp; Compliance (aud_).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Regista o módulo PlatformGovernance — governance + auditcompliance como contexto unificado.
    /// Substitui as chamadas separadas AddGovernanceModule + AddAuditModule.
    /// </summary>
    public static IServiceCollection AddPlatformGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGovernanceApplication(configuration);
        services.AddGovernanceInfrastructure(configuration);
        services.AddAuditApplication(configuration);
        services.AddAuditInfrastructure(configuration);
        return services;
    }

    /// <summary>Mantido por compatibilidade — preferir AddPlatformGovernanceModule.</summary>
    public static IServiceCollection AddGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddPlatformGovernanceModule(configuration);
}
