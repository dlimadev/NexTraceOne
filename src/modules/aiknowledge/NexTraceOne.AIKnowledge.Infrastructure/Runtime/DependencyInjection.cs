using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Infrastructure.Context;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Anthropic;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.LmStudio;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Ollama;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.OpenAI;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.Knowledge.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime;

/// <summary>
/// Registra serviços de infraestrutura do módulo AI Runtime.
/// Inclui: Ollama provider, OpenAI provider (quando configurado), factory, catalog, health services,
/// tool registry, tool executor, tool permission validator.
/// Registra também DbContexts de leitura para grounding cross-módulo (P01.10).
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
        services.Configure<AnthropicOptions>(
            configuration.GetSection(AnthropicOptions.SectionName));
        services.Configure<LmStudioOptions>(
            configuration.GetSection(LmStudioOptions.SectionName));

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
        }).AddStandardResilienceHandler(opts =>
        {
            // Inferência local pode ser lenta — aumentar timeouts por tentativa
            opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
            opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);
            opts.Retry.MaxRetryAttempts = 2;
            // SamplingDuration must be >= 2× AttemptTimeout per Polly validation
            opts.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(300);
        });

        // Ollama provider — scoped because OllamaHttpClient is transient (from HttpClientFactory)
        services.AddScoped<OllamaProvider>();
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<OllamaProvider>());
        services.AddScoped<IChatCompletionProvider>(sp => sp.GetRequiredService<OllamaProvider>());
        services.AddScoped<OllamaEmbeddingProvider>();
        services.AddScoped<IEmbeddingProvider>(sp => sp.GetRequiredService<OllamaEmbeddingProvider>());

        // OpenAI provider — registered only when ApiKey is configured
        var openAiOptions = configuration.GetSection(OpenAiOptions.SectionName).Get<OpenAiOptions>();
        if (openAiOptions?.IsConfigured == true)
        {
            services.AddHttpClient<OpenAiHttpClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(openAiOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(openAiOptions.TimeoutSeconds);
            }).AddStandardResilienceHandler(opts =>
            {
                opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(90);
                opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
                opts.Retry.MaxRetryAttempts = 2;
            });

            services.AddScoped<OpenAiProvider>();
            services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<OpenAiProvider>());
            services.AddScoped<IChatCompletionProvider>(sp => sp.GetRequiredService<OpenAiProvider>());
            services.AddScoped<OpenAiEmbeddingProvider>();
            services.AddScoped<IEmbeddingProvider>(sp => sp.GetRequiredService<OpenAiEmbeddingProvider>());
        }

        // Anthropic provider — registered only when ApiKey is configured
        var anthropicOptions = configuration.GetSection(AnthropicOptions.SectionName).Get<AnthropicOptions>();
        if (anthropicOptions?.IsConfigured == true)
        {
            services.AddHttpClient<AnthropicHttpClient>((sp, client) =>
            {
                var normalized = anthropicOptions.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                    ? anthropicOptions.BaseUrl
                    : $"{anthropicOptions.BaseUrl}/";
                client.BaseAddress = new Uri(normalized);
                client.Timeout = TimeSpan.FromSeconds(anthropicOptions.TimeoutSeconds);
            }).AddStandardResilienceHandler(opts =>
            {
                opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(90);
                opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
                opts.Retry.MaxRetryAttempts = 2;
            });

            services.AddScoped<AnthropicProvider>();
            services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<AnthropicProvider>());
            services.AddScoped<IChatCompletionProvider>(sp => sp.GetRequiredService<AnthropicProvider>());
        }

        // LM Studio provider — registered only when Enabled = true in configuration
        var lmStudioOptions = configuration.GetSection(LmStudioOptions.SectionName).Get<LmStudioOptions>();
        if (lmStudioOptions?.Enabled == true)
        {
            services.AddHttpClient<LmStudioHttpClient>((sp, client) =>
            {
                var normalized = lmStudioOptions.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                    ? lmStudioOptions.BaseUrl
                    : $"{lmStudioOptions.BaseUrl}/";
                client.BaseAddress = new Uri(normalized);
                client.Timeout = TimeSpan.FromSeconds(lmStudioOptions.TimeoutSeconds);
            }).AddStandardResilienceHandler(opts =>
            {
                opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
                opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);
                opts.Retry.MaxRetryAttempts = 2;
                // SamplingDuration must be >= 2× AttemptTimeout per Polly validation
                opts.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(300);
            });

            services.AddScoped<LmStudioProvider>();
            services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<LmStudioProvider>());
            services.AddScoped<IChatCompletionProvider>(sp => sp.GetRequiredService<LmStudioProvider>());
        }

        // Factory — scoped to resolve scoped providers
        services.AddScoped<IAiProviderFactory, AiProviderFactory>();

        // External AI routing port adapter
        services.AddScoped<IExternalAIRoutingPort, ExternalAiRoutingPortAdapter>();

        // Catalog — scoped (depends on scoped repository)
        services.AddScoped<IAiModelCatalogService, AiModelCatalogService>();

        // Health — scoped (depends on scoped factory)
        services.AddScoped<IAiProviderHealthService, AiProviderHealthService>();

        // Token quota cache — singleton (shared across requests, thread-safe via MemoryCache)
        // AI-0.2: usa DistributedTokenQuotaCache quando Redis está configurado; fallback in-memory.
        services.Configure<TokenQuotaCacheOptions>(
            configuration.GetSection(TokenQuotaCacheOptions.SectionName));
        var redisCs = configuration["ConnectionStrings:Redis"];
        if (!string.IsNullOrWhiteSpace(redisCs))
            services.AddSingleton<ITokenQuotaCache, DistributedTokenQuotaCache>();
        else
            services.AddSingleton<ITokenQuotaCache, InMemoryTokenQuotaCache>();

        // Token quota — scoped (depends on scoped repositories)
        services.AddScoped<IAiTokenQuotaService, AiTokenQuotaService>();

        // Source registry — scoped (depends on scoped repository)
        services.AddScoped<IAiSourceRegistryService, AiSourceRegistryService>();

        // ── Cross-module DbContexts for AI grounding (read-only) ────────
        // These DbContexts are already registered by their owning modules.
        // We register them conditionally here only if not already registered,
        // using the same connection string as the owning module.
        RegisterCrossModuleContextIfNeeded<CatalogGraphDbContext>(
            services, configuration, "CatalogDatabase");
        RegisterCrossModuleContextIfNeeded<ChangeIntelligenceDbContext>(
            services, configuration, "ChangeIntelligenceDatabase");
        RegisterCrossModuleContextIfNeeded<IncidentDbContext>(
            services, configuration, "IncidentDatabase");
        RegisterCrossModuleContextIfNeeded<KnowledgeDbContext>(
            services, configuration, "KnowledgeDatabase");
        RegisterCrossModuleContextIfNeeded<ContractsDbContext>(
            services, configuration, "ContractsDatabase");

        // Cross-module grounding readers — thin abstractions over read-only DbContext access
        services.AddScoped<ICatalogGroundingReader, CatalogGroundingReader>();
        services.AddScoped<IChangeGroundingReader, ChangeGroundingReader>();
        services.AddScoped<IIncidentGroundingReader, IncidentGroundingReader>();
        services.AddScoped<IKnowledgeDocumentGroundingReader, KnowledgeDocumentGroundingReader>();
        services.AddScoped<IContractGroundingReader, ContractGroundingReader>();
        services.AddScoped<IServiceInterfaceGroundingReader, ServiceInterfaceGroundingReader>();

        // Retrieval services — scoped (foundation for RAG, database grounding and telemetry)
        services.AddScoped<IDocumentRetrievalService, DocumentRetrievalService>();
        services.AddScoped<IDatabaseRetrievalService, DatabaseRetrievalService>();
        services.AddScoped<ITelemetryRetrievalService, TelemetryRetrievalService>();

        // Embedding cache — singleton para partilhar cache entre requests dentro do processo.
        // Usa IServiceScopeFactory para resolver IEmbeddingProvider no momento de uso (evita captive dependency).
        services.AddSingleton<NexTraceOne.AIKnowledge.Application.Governance.Abstractions.IEmbeddingCacheService>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<
                NexTraceOne.AIKnowledge.Infrastructure.Governance.Services.InMemoryEmbeddingCacheService>>();
            return new NexTraceOne.AIKnowledge.Infrastructure.Governance.Services.InMemoryEmbeddingCacheService(
                new NexTraceOne.AIKnowledge.Infrastructure.Governance.Services.ScopedEmbeddingProviderProxy(scopeFactory),
                logger);
        });

        // ── Tool infrastructure ──────────────────────────────────────────
        services.AddSingleton<IAgentTool, ListServicesInfoTool>();
        services.AddSingleton<IAgentTool, GetServiceHealthTool>();
        // Scoped tools that depend on scoped repositories/readers
        services.AddScoped<IAgentTool, ListRecentChangesTool>();
        services.AddScoped<IAgentTool, GetContractDetailsTool>();
        services.AddScoped<IAgentTool, SearchIncidentsTool>();
        services.AddScoped<IAgentTool, GetTokenUsageSummaryTool>();
        services.AddScoped<IAgentTool, SearchKnowledgeTool>();
        services.AddScoped<IAgentTool, GetRunbookTool>();
        services.AddScoped<IAgentTool, ListContractVersionsTool>();
        // ── AI-4.2: 6 new cross-module tools ────────────────────────────
        services.AddScoped<IAgentTool, GetKnowledgeDocsTool>();
        services.AddScoped<IAgentTool, TriggerBlastRadiusTool>();
        services.AddScoped<IAgentTool, GetEvidencePackTool>();
        services.AddScoped<IAgentTool, CheckSloStatusTool>();
        services.AddScoped<IAgentTool, GetComplianceStatusTool>();
        services.AddScoped<IAgentTool, GetCostContextTool>();
        // Registry is Scoped (not Singleton) so that it can capture scoped tool instances
        services.AddScoped<IToolRegistry, InMemoryToolRegistry>();
        services.AddScoped<IToolExecutor, AgentToolExecutor>();
        services.AddScoped<IToolPermissionValidator, AllowedToolsPermissionValidator>();

        // W4-03: AI Resource Governor — wraps all IChatCompletionProvider instances with
        // SemaphoreSlim concurrency control and circuit breaker.
        services.AddSingleton<AiResourceGovernor>();
        var providerDescriptors = services
            .Where(d => d.ServiceType == typeof(IChatCompletionProvider))
            .ToList();
        foreach (var descriptor in providerDescriptors)
            services.Remove(descriptor);
        foreach (var descriptor in providerDescriptors)
        {
            var captured = descriptor;
            services.Add(ServiceDescriptor.Scoped<IChatCompletionProvider>(sp =>
            {
                var inner = (IChatCompletionProvider)captured.ImplementationFactory!(sp);
                return new AiResourceGovernorInterceptor(
                    inner,
                    sp.GetRequiredService<AiResourceGovernor>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AiResourceGovernorInterceptor>>());
            }));
        }

        return services;
    }

    /// <summary>
    /// Regista um DbContext cross-módulo se ainda não estiver registado pelo módulo dono.
    /// Usa a connection string do módulo dono; se não existir, ignora silenciosamente.
    /// </summary>
    private static void RegisterCrossModuleContextIfNeeded<TContext>(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionStringKey)
        where TContext : DbContext
    {
        // Skip if already registered by the owning module
        if (services.Any(d => d.ServiceType == typeof(TContext)))
            return;

        var connectionString = configuration.GetConnectionString(connectionStringKey)
            ?? configuration[$"ConnectionStrings:{connectionStringKey}"];

        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        services.AddDbContext<TContext>((_, options) =>
            options.UseNpgsql(connectionString));
    }
}
