using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.InProcess;
using NexTraceOne.BuildingBlocks.Infrastructure.Http;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.BuildingBlocks.Infrastructure.MultiTenancy;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.BuildingBlocks.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura compartilhados: Interceptors, Converters, Outbox, EventBus.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registra interceptors, converters e demais serviços de infraestrutura.</summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TenantRlsInterceptor>();
        services.AddTransient<AirGapHttpMessageHandler>();
        services.AddSingleton<HttpClientConfiguration>();

        return services;
    }

    /// <summary>Registra o barramento de eventos compartilhado da plataforma.</summary>
    public static IServiceCollection AddBuildingBlocksEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IEventBus, InProcessEventBus>();

        return services;
    }

    /// <summary>
    /// Registra o BuildingBlocksDbContext (tabela bb_dead_letter_messages) e o IDeadLetterRepository.
    /// Deve ser chamado em ApiHost e BackgroundWorkers.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BuildingBlocks")
            ?? configuration.GetConnectionString("Default")
            ?? string.Empty;

        services.AddDbContext<BuildingBlocksDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();

        return services;
    }

    /// <summary>
    /// Registra o TenantSchemaManager para isolamento schema-per-tenant no PostgreSQL.
    /// Requer uma connection string administrativa com permissão para CREATE SCHEMA.
    /// </summary>
    public static IServiceCollection AddTenantSchemaManager(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<ITenantSchemaManager>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TenantSchemaManager>>();
            return new TenantSchemaManager(connectionString, logger);
        });

        return services;
    }
}
