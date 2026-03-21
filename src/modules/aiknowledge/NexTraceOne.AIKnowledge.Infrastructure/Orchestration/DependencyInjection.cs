using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Context;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration;

/// <summary>
/// Registra serviços de infraestrutura do módulo AiOrchestration.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiOrchestrationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("AiOrchestrationDatabase", "NexTraceOne");

        services.AddDbContext<AiOrchestrationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);

            if (string.Equals(
                Environment.GetEnvironmentVariable("NEXTRACE_IGNORE_PENDING_MODEL_CHANGES"),
                "true",
                StringComparison.OrdinalIgnoreCase))
            {
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditInterceptor>(),
                serviceProvider.GetRequiredService<TenantRlsInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AiOrchestrationDbContext>());

        // Fase 2 — AI Context Builders
        services.AddScoped<IAIContextBuilder, AIContextBuilder>();
        services.AddScoped<IPromotionRiskContextBuilder, PromotionRiskContextBuilder>();

        // Fase 2 — Repositórios de Orquestração
        services.AddScoped<IAiOrchestrationConversationRepository, AiOrchestrationConversationRepository>();
        services.AddScoped<IKnowledgeCaptureEntryRepository, KnowledgeCaptureEntryRepository>();
        services.AddScoped<IGeneratedTestArtifactRepository, GeneratedTestArtifactRepository>();

        return services;
    }
}
