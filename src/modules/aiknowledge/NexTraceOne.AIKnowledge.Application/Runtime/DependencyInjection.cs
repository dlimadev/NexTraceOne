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
        // MediatR handlers are auto-discovered by assembly scanning in the Governance DI
        return services;
    }
}
