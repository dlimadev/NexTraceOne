using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Ollama;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.OpenAI;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime;

/// <summary>
/// Registra serviços de infraestrutura do módulo AI Runtime.
/// Inclui: Ollama provider, OpenAI provider (quando configurado), factory, catalog, health services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiRuntimeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<OllamaOptions>(
            configuration.GetSection(OllamaOptions.SectionName));
        services.Configure<AiRoutingOptions>(
            configuration.GetSection(AiRoutingOptions.SectionName));
        services.Configure<OpenAiOptions>(
            configuration.GetSection(OpenAiOptions.SectionName));

        // Ollama HTTP client (registered via IHttpClientFactory — typed client pattern)
        services.AddHttpClient<OllamaHttpClient>((sp, client) =>
        {
            var options = configuration.GetSection(OllamaOptions.SectionName).Get<OllamaOptions>()
                ?? new OllamaOptions();
            var normalizedBaseUrl = options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                ? options.BaseUrl
                : $"{options.BaseUrl}/";

            client.BaseAddress = new Uri(normalizedBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // Ollama provider — scoped because OllamaHttpClient is transient (from HttpClientFactory)
        services.AddScoped<OllamaProvider>();
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<OllamaProvider>());
        services.AddScoped<IChatCompletionProvider>(sp => sp.GetRequiredService<OllamaProvider>());

        // OpenAI provider — registered only when ApiKey is configured
        var openAiOptions = configuration.GetSection(OpenAiOptions.SectionName).Get<OpenAiOptions>();
        if (openAiOptions?.IsConfigured == true)
        {
            services.AddHttpClient<OpenAiHttpClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(openAiOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(openAiOptions.TimeoutSeconds);
            });

            services.AddScoped<OpenAiProvider>();
            services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<OpenAiProvider>());
            services.AddScoped<IChatCompletionProvider>(sp => sp.GetRequiredService<OpenAiProvider>());
        }

        // Factory — scoped to resolve scoped providers
        services.AddScoped<IAiProviderFactory, AiProviderFactory>();

        // External AI routing port adapter
        services.AddScoped<IExternalAIRoutingPort, ExternalAiRoutingPortAdapter>();

        // Catalog — scoped (depends on scoped repository)
        services.AddScoped<IAiModelCatalogService, AiModelCatalogService>();

        // Health — scoped (depends on scoped factory)
        services.AddScoped<IAiProviderHealthService, AiProviderHealthService>();

        // Token quota — scoped (depends on scoped repositories)
        services.AddScoped<IAiTokenQuotaService, AiTokenQuotaService>();

        // Source registry — scoped (depends on scoped repository)
        services.AddScoped<IAiSourceRegistryService, AiSourceRegistryService>();

        // Retrieval services — scoped (foundation for RAG, database grounding and telemetry)
        services.AddScoped<IDocumentRetrievalService, DocumentRetrievalService>();
        services.AddScoped<IDatabaseRetrievalService, DatabaseRetrievalService>();
        services.AddScoped<ITelemetryRetrievalService, TelemetryRetrievalService>();

        return services;
    }
}
