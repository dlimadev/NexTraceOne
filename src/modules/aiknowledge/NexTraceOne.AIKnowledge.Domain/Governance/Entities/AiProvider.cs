using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa um provedor de IA registado na plataforma (ex: Ollama, OpenAI, Gemini, Azure OpenAI).
/// Cada provedor possui tipo, URL base, capacidades suportadas e estado de ativação
/// controlado por governança.
///
/// Invariantes:
/// - Nome, Slug e ProviderType são obrigatórios e imutáveis após registo.
/// - IsExternal é derivado de !IsLocal no momento do registo.
/// - Priority deve ser positivo (quanto menor, maior a prioridade).
/// - Provedor inicia sempre com IsEnabled = true e HealthStatus = Unknown.
/// - TimeoutSeconds deve ser positivo (padrão: 30).
/// </summary>
public sealed class AiProvider : AuditableEntity<AiProviderId>
{
    private AiProvider() { }

    /// <summary>Nome técnico do provedor (ex: "ollama-local", "openai-prod").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Slug URL-friendly único do provedor (ex: "ollama", "openai").</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Nome de exibição amigável para a interface do utilizador.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Tipo do provedor (ex: "Ollama", "OpenAI", "AzureOpenAI", "Gemini").</summary>
    public string ProviderType { get; private set; } = string.Empty;

    /// <summary>URL base para comunicação com o provedor.</summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>Indica se o provedor é local/interno à infraestrutura da organização.</summary>
    public bool IsLocal { get; private set; }

    /// <summary>Indica se o provedor é externo (derivado de !IsLocal).</summary>
    public bool IsExternal { get; private set; }

    /// <summary>Indica se o provedor está ativo e disponível para utilização.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Modo de autenticação do provedor.</summary>
    public AuthenticationMode AuthenticationMode { get; private set; }

    /// <summary>
    /// Capacidades suportadas pelo provedor, separadas por vírgula
    /// (ex: "chat,embeddings,vision,tool-calling,streaming,structured-output").
    /// </summary>
    public string SupportedCapabilities { get; private set; } = string.Empty;

    /// <summary>Indica se o provedor suporta chat/conversação.</summary>
    public bool SupportsChat { get; private set; }

    /// <summary>Indica se o provedor suporta geração de embeddings.</summary>
    public bool SupportsEmbeddings { get; private set; }

    /// <summary>Indica se o provedor suporta tool/function calling.</summary>
    public bool SupportsTools { get; private set; }

    /// <summary>Indica se o provedor suporta entrada de imagens (vision).</summary>
    public bool SupportsVision { get; private set; }

    /// <summary>Indica se o provedor suporta saída estruturada (JSON mode).</summary>
    public bool SupportsStructuredOutput { get; private set; }

    /// <summary>Estado de saúde persistido, atualizado pelo health check periódico.</summary>
    public ProviderHealthStatus HealthStatus { get; private set; }

    /// <summary>Prioridade de seleção do provedor (menor valor = maior prioridade).</summary>
    public int Priority { get; private set; }

    /// <summary>Timeout em segundos para chamadas ao provedor.</summary>
    public int TimeoutSeconds { get; private set; }

    /// <summary>Descrição operacional do provedor.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o provedor foi registado na plataforma.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>Optimistic concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Regista um novo provedor de IA com validações de invariantes.
    /// O provedor inicia ativo e IsExternal é derivado automaticamente de !IsLocal.
    /// HealthStatus inicia como Unknown até o primeiro health check.
    /// </summary>
    public static AiProvider Register(
        string name,
        string displayName,
        string providerType,
        string baseUrl,
        bool isLocal,
        string supportedCapabilities,
        int priority,
        string description,
        DateTimeOffset registeredAt,
        string? slug = null,
        AuthenticationMode authenticationMode = AuthenticationMode.None,
        bool supportsChat = false,
        bool supportsEmbeddings = false,
        bool supportsTools = false,
        bool supportsVision = false,
        bool supportsStructuredOutput = false,
        int timeoutSeconds = 30)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(providerType);
        Guard.Against.NullOrWhiteSpace(baseUrl);
        Guard.Against.NullOrWhiteSpace(supportedCapabilities);
        Guard.Against.NegativeOrZero(priority);
        Guard.Against.NegativeOrZero(timeoutSeconds);

        return new AiProvider
        {
            Id = AiProviderId.New(),
            Name = name,
            Slug = slug ?? name.ToLowerInvariant().Replace(' ', '-'),
            DisplayName = displayName,
            ProviderType = providerType,
            BaseUrl = baseUrl,
            IsLocal = isLocal,
            IsExternal = !isLocal,
            IsEnabled = true,
            AuthenticationMode = authenticationMode,
            SupportedCapabilities = supportedCapabilities,
            SupportsChat = supportsChat,
            SupportsEmbeddings = supportsEmbeddings,
            SupportsTools = supportsTools,
            SupportsVision = supportsVision,
            SupportsStructuredOutput = supportsStructuredOutput,
            HealthStatus = ProviderHealthStatus.Unknown,
            Priority = priority,
            TimeoutSeconds = timeoutSeconds,
            Description = description ?? string.Empty,
            RegisteredAt = registeredAt
        };
    }

    /// <summary>
    /// Atualiza a configuração do provedor.
    /// Permite ajustar nome de exibição, URL, capacidades, prioridade e descrição.
    /// </summary>
    public Result<Unit> UpdateConfiguration(
        string displayName,
        string baseUrl,
        string supportedCapabilities,
        int priority,
        string description)
    {
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(baseUrl);
        Guard.Against.NullOrWhiteSpace(supportedCapabilities);
        Guard.Against.NegativeOrZero(priority);

        DisplayName = displayName;
        BaseUrl = baseUrl;
        SupportedCapabilities = supportedCapabilities;
        Priority = priority;
        Description = description ?? string.Empty;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza as flags de capacidade do provedor.
    /// </summary>
    public Result<Unit> UpdateCapabilityFlags(
        bool supportsChat,
        bool supportsEmbeddings,
        bool supportsTools,
        bool supportsVision,
        bool supportsStructuredOutput)
    {
        SupportsChat = supportsChat;
        SupportsEmbeddings = supportsEmbeddings;
        SupportsTools = supportsTools;
        SupportsVision = supportsVision;
        SupportsStructuredOutput = supportsStructuredOutput;
        return Unit.Value;
    }

    /// <summary>
    /// Regista o resultado do último health check do provedor.
    /// </summary>
    public void RecordHealthStatus(ProviderHealthStatus status)
    {
        HealthStatus = status;
    }

    /// <summary>
    /// Ativa o provedor, tornando-o disponível para utilização.
    /// Operação idempotente — não retorna erro se já ativo.
    /// </summary>
    public Result<Unit> Enable()
    {
        IsEnabled = true;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa o provedor, removendo-o do pool de provedores disponíveis.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Disable()
    {
        IsEnabled = false;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AiProvider.</summary>
public sealed record AiProviderId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiProviderId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiProviderId From(Guid id) => new(id);
}
