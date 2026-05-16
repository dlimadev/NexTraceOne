using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.ValueObjects;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa um modelo de IA registrado no Model Registry da plataforma.
/// Cada modelo possui tipo funcional, provedor, nível de sensibilidade e estado
/// de ciclo de vida controlado por governança.
///
/// Invariantes:
/// - Nome e provedor são obrigatórios e imutáveis após registo.
/// - SensitivityLevel deve estar entre 1 e 5.
/// - Modelo inicia sempre com status Active.
/// - IsExternal é derivado de !IsInternal no momento do registo.
/// - ProviderId é FK opcional para AiProvider (preferido ao string Provider).
/// </summary>
public sealed class AIModel : AuditableEntity<AIModelId>
{
    private AIModel() { }

    /// <summary>Nome técnico do modelo (ex: "gpt-4o", "claude-3-opus").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Slug URL-friendly único do modelo (ex: "deepseek-r1-1-5b").</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Nome de exibição amigável para a interface do utilizador.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Provedor do modelo (ex: "OpenAI", "Anthropic", "Internal").</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>FK opcional para a entidade AiProvider (preferido ao campo string Provider).</summary>
    public AiProviderId? ProviderId { get; private set; }

    /// <summary>Identificador do modelo no provedor externo (ex: "deepseek-r1:1.5b" no Ollama).</summary>
    public string ExternalModelId { get; private set; } = string.Empty;

    /// <summary>Tipo funcional do modelo — determina os contextos de utilização.</summary>
    public ModelType ModelType { get; private set; }

    /// <summary>Categoria funcional do modelo (ex: "general", "code", "reasoning", "embeddings").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Indica se o modelo é interno/local à organização.</summary>
    public bool IsInternal { get; private set; }

    /// <summary>Indica se o modelo é externo (derivado de !IsInternal).</summary>
    public bool IsExternal { get; private set; }

    /// <summary>Indica se o modelo está instalado/disponível no provedor (ex: pulled no Ollama).</summary>
    public bool IsInstalled { get; private set; }

    /// <summary>Estado atual do ciclo de vida do modelo.</summary>
    public ModelStatus Status { get; private set; }

    /// <summary>Capacidades do modelo separadas por vírgula (ex: "chat,code,reasoning").</summary>
    public string Capabilities { get; private set; } = string.Empty;

    /// <summary>Casos de uso padrão recomendados para este modelo.</summary>
    public string DefaultUseCases { get; private set; } = string.Empty;

    /// <summary>Nível de sensibilidade (1 = baixo, 5 = máximo) — influencia políticas de acesso.</summary>
    public int SensitivityLevel { get; private set; }

    // ── Default flags ─────────────────────────────────────────────────────

    /// <summary>Indica se é o modelo default para chat/conversação.</summary>
    public bool IsDefaultForChat { get; private set; }

    /// <summary>Indica se é o modelo default para tarefas de raciocínio avançado.</summary>
    public bool IsDefaultForReasoning { get; private set; }

    /// <summary>Indica se é o modelo default para geração de embeddings.</summary>
    public bool IsDefaultForEmbeddings { get; private set; }

    // ── Capability flags ──────────────────────────────────────────────────

    /// <summary>Indica se o modelo suporta streaming de resposta.</summary>
    public bool SupportsStreaming { get; private set; }

    /// <summary>Indica se o modelo suporta tool/function calling.</summary>
    public bool SupportsToolCalling { get; private set; }

    /// <summary>Indica se o modelo suporta geração de embeddings.</summary>
    public bool SupportsEmbeddings { get; private set; }

    /// <summary>Indica se o modelo suporta entrada de imagens (vision).</summary>
    public bool SupportsVision { get; private set; }

    /// <summary>Indica se o modelo suporta saída estruturada (JSON mode).</summary>
    public bool SupportsStructuredOutput { get; private set; }

    // ── Hardware & sizing ─────────────────────────────────────────────────

    /// <summary>Tamanho do context window em tokens (null se desconhecido).</summary>
    public int? ContextWindow { get; private set; }

    /// <summary>Indica se o modelo requer GPU para inferência adequada.</summary>
    public bool RequiresGpu { get; private set; }

    /// <summary>RAM recomendada em GB para execução local (null se não aplicável).</summary>
    public decimal? RecommendedRamGb { get; private set; }

    // ── License & compliance ──────────────────────────────────────────────

    /// <summary>Nome da licença do modelo (ex: "Apache 2.0", "MIT", "Proprietary").</summary>
    public string LicenseName { get; private set; } = string.Empty;

    /// <summary>URL da licença do modelo.</summary>
    public string LicenseUrl { get; private set; } = string.Empty;

    /// <summary>Estado de compliance do modelo (ex: "approved", "pending-review", "restricted").</summary>
    public string ComplianceStatus { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o modelo foi registrado no Model Registry.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>Optimistic concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Registra um novo modelo de IA no Model Registry com validações de invariantes.
    /// O modelo inicia com status Active e IsExternal é derivado automaticamente.
    /// </summary>
    public static AIModel Register(
        string name,
        string displayName,
        string provider,
        ModelType modelType,
        bool isInternal,
        string capabilities,
        int sensitivityLevel,
        DateTimeOffset registeredAt,
        string? slug = null,
        AiProviderId? providerId = null,
        string? externalModelId = null,
        string? category = null,
        bool isInstalled = false,
        bool isDefaultForChat = false,
        bool isDefaultForReasoning = false,
        bool isDefaultForEmbeddings = false,
        bool supportsStreaming = false,
        bool supportsToolCalling = false,
        bool supportsEmbeddings = false,
        bool supportsVision = false,
        bool supportsStructuredOutput = false,
        int? contextWindow = null,
        bool requiresGpu = false,
        decimal? recommendedRamGb = null,
        string? licenseName = null,
        string? licenseUrl = null,
        string? complianceStatus = null)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(provider);
        Guard.Against.NullOrWhiteSpace(capabilities);
        Guard.Against.OutOfRange(sensitivityLevel, nameof(sensitivityLevel), 1, 5);

        return new AIModel
        {
            Id = AIModelId.New(),
            Name = name,
            Slug = SlugHelper.Derive(name, slug),
            DisplayName = displayName,
            Provider = provider,
            ProviderId = providerId,
            ExternalModelId = externalModelId ?? name,
            ModelType = modelType,
            Category = category ?? string.Empty,
            IsInternal = isInternal,
            IsExternal = !isInternal,
            IsInstalled = isInstalled,
            Status = ModelStatus.Active,
            Capabilities = capabilities,
            DefaultUseCases = string.Empty,
            SensitivityLevel = sensitivityLevel,
            IsDefaultForChat = isDefaultForChat,
            IsDefaultForReasoning = isDefaultForReasoning,
            IsDefaultForEmbeddings = isDefaultForEmbeddings,
            SupportsStreaming = supportsStreaming,
            SupportsToolCalling = supportsToolCalling,
            SupportsEmbeddings = supportsEmbeddings,
            SupportsVision = supportsVision,
            SupportsStructuredOutput = supportsStructuredOutput,
            ContextWindow = contextWindow,
            RequiresGpu = requiresGpu,
            RecommendedRamGb = recommendedRamGb,
            LicenseName = licenseName ?? string.Empty,
            LicenseUrl = licenseUrl ?? string.Empty,
            ComplianceStatus = complianceStatus ?? string.Empty,
            RegisteredAt = registeredAt
        };
    }

    /// <summary>
    /// Atualiza os detalhes configuráveis do modelo.
    /// Permite ajustar nome de exibição, capacidades, casos de uso e nível de sensibilidade.
    /// </summary>
    public Result<Unit> UpdateDetails(
        string displayName,
        string capabilities,
        string defaultUseCases,
        int sensitivityLevel)
    {
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(capabilities);
        Guard.Against.OutOfRange(sensitivityLevel, nameof(sensitivityLevel), 1, 5);

        DisplayName = displayName;
        Capabilities = capabilities;
        DefaultUseCases = defaultUseCases ?? string.Empty;
        SensitivityLevel = sensitivityLevel;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza as flags de capacidade do modelo.
    /// </summary>
    public Result<Unit> UpdateCapabilityFlags(
        bool supportsStreaming,
        bool supportsToolCalling,
        bool supportsEmbeddings,
        bool supportsVision,
        bool supportsStructuredOutput)
    {
        SupportsStreaming = supportsStreaming;
        SupportsToolCalling = supportsToolCalling;
        SupportsEmbeddings = supportsEmbeddings;
        SupportsVision = supportsVision;
        SupportsStructuredOutput = supportsStructuredOutput;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza as flags de modelo default por tipo de tarefa.
    /// </summary>
    public Result<Unit> SetDefaultFlags(
        bool isDefaultForChat,
        bool isDefaultForReasoning,
        bool isDefaultForEmbeddings)
    {
        IsDefaultForChat = isDefaultForChat;
        IsDefaultForReasoning = isDefaultForReasoning;
        IsDefaultForEmbeddings = isDefaultForEmbeddings;
        return Unit.Value;
    }

    /// <summary>
    /// Marca o modelo como instalado/disponível no provedor.
    /// </summary>
    public void MarkAsInstalled() => IsInstalled = true;

    /// <summary>
    /// Marca o modelo como não instalado/indisponível no provedor.
    /// </summary>
    public void MarkAsUninstalled() => IsInstalled = false;

    /// <summary>
    /// Ativa o modelo, tornando-o disponível para utilização.
    /// Operação idempotente — não retorna erro se já ativo.
    /// </summary>
    public Result<Unit> Activate()
    {
        Status = ModelStatus.Active;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa o modelo temporariamente, removendo-o do pool de modelos disponíveis.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Deactivate()
    {
        Status = ModelStatus.Inactive;
        return Unit.Value;
    }

    /// <summary>
    /// Marca o modelo como depreciado — sinaliza remoção futura.
    /// Modelo depreciado ainda pode ser utilizado mas gera avisos.
    /// </summary>
    public Result<Unit> Deprecate()
    {
        Status = ModelStatus.Deprecated;
        return Unit.Value;
    }

    /// <summary>
    /// Bloqueia o modelo por política de governança — uso proibido.
    /// Modelo bloqueado é rejeitado em todas as avaliações de política.
    /// </summary>
    public Result<Unit> Block()
    {
        Status = ModelStatus.Blocked;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AIModel.</summary>
public sealed record AIModelId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AIModelId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AIModelId From(Guid id) => new(id);
}
