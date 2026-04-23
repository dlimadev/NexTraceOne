using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Knowledge.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Knowledge ao contêiner de DI.</summary>
    public static IServiceCollection AddKnowledgeApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Wave AY — Organizational Knowledge & Documentation Intelligence ─
        services.AddSingleton<IKnowledgeBaseUtilizationReader, NullKnowledgeBaseUtilizationReader>();
        services.AddSingleton<ITeamKnowledgeSharingReader, NullTeamKnowledgeSharingReader>();

        return services;
    }
}
