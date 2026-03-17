using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.AIKnowledge.Application.Runtime;

/// <summary>
/// Registra serviços da camada Application do módulo AI Runtime.
/// Inclui: MediatR handlers e validators para inferência, providers e catálogo.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiRuntimeApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MediatR handlers in this assembly are auto-discovered via AddMediatR assembly scanning
        // registered in the module's root DependencyInjection (same assembly as Governance handlers).
        return services;
    }
}
