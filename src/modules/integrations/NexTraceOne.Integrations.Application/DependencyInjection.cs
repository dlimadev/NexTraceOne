using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.GetIngestionFreshness;
using NexTraceOne.Integrations.Application.Features.GetIntegrationConnector;
using NexTraceOne.Integrations.Application.Features.ListIngestionExecutions;
using NexTraceOne.Integrations.Application.Features.ListIngestionSources;
using NexTraceOne.Integrations.Application.Features.ListIntegrationConnectors;
using NexTraceOne.Integrations.Application.Parsing;

namespace NexTraceOne.Integrations.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Integrations.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Integrations ao contêiner de DI.</summary>
    public static IServiceCollection AddIntegrationsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddSingleton<IIngestionPayloadParser, GenericIngestionPayloadParser>();

        // ── Query Validators ─────────────────────────────────────────────
        services.AddTransient<IValidator<GetIntegrationConnector.Query>, GetIntegrationConnector.Validator>();
        services.AddTransient<IValidator<ListIngestionExecutions.Query>, ListIngestionExecutions.Validator>();
        services.AddTransient<IValidator<ListIngestionSources.Query>, ListIngestionSources.Validator>();
        services.AddTransient<IValidator<GetIngestionFreshness.Query>, GetIngestionFreshness.Validator>();
        services.AddTransient<IValidator<ListIntegrationConnectors.Query>, ListIntegrationConnectors.Validator>();

        return services;
    }
}
