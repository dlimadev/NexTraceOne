using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Application.Runtime;

/// <summary>
/// Registra serviços da camada Application do módulo AI Runtime.
/// Inclui: MediatR handlers, validators, StructuredOutputFallback e ToolCallRouter.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiRuntimeApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // StructuredOutputFallback + ToolCallRouter — Singleton (stateless, thread-safe)
        services.AddSingleton<StructuredOutputFallback>();
        services.AddSingleton<IToolCallRouter, ToolCallRouter>();

        // MediatR handlers in this assembly are auto-discovered via AddMediatR assembly scanning
        // registered in the module's root DependencyInjection (same assembly as Governance handlers).
        return services;
    }
}
