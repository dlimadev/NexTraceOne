using Ardalis.GuardClauses;

using MediatR;

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
/// - Nome e ProviderType são obrigatórios e imutáveis após registo.
/// - IsExternal é derivado de !IsLocal no momento do registo.
/// - Priority deve ser positivo (quanto menor, maior a prioridade).
/// - Provedor inicia sempre com IsEnabled = true.
/// </summary>
public sealed class AiProvider : AuditableEntity<AiProviderId>
{
    private AiProvider() { }

    /// <summary>Nome técnico do provedor (ex: "ollama-local", "openai-prod").</summary>
    public string Name { get; private set; } = string.Empty;

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

    /// <summary>
    /// Capacidades suportadas pelo provedor, separadas por vírgula
    /// (ex: "chat,embeddings,vision,tool-calling,streaming,structured-output").
    /// </summary>
    public string SupportedCapabilities { get; private set; } = string.Empty;

    /// <summary>Prioridade de seleção do provedor (menor valor = maior prioridade).</summary>
    public int Priority { get; private set; }

    /// <summary>Descrição operacional do provedor.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o provedor foi registado na plataforma.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>
    /// Regista um novo provedor de IA com validações de invariantes.
    /// O provedor inicia ativo e IsExternal é derivado automaticamente de !IsLocal.
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
        DateTimeOffset registeredAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(providerType);
        Guard.Against.NullOrWhiteSpace(baseUrl);
        Guard.Against.NullOrWhiteSpace(supportedCapabilities);
        Guard.Against.NegativeOrZero(priority);

        return new AiProvider
        {
            Id = AiProviderId.New(),
            Name = name,
            DisplayName = displayName,
            ProviderType = providerType,
            BaseUrl = baseUrl,
            IsLocal = isLocal,
            IsExternal = !isLocal,
            IsEnabled = true,
            SupportedCapabilities = supportedCapabilities,
            Priority = priority,
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
