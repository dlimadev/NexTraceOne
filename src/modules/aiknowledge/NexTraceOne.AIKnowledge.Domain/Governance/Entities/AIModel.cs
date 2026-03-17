using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
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
/// </summary>
public sealed class AIModel : AuditableEntity<AIModelId>
{
    private AIModel() { }

    /// <summary>Nome técnico do modelo (ex: "gpt-4o", "claude-3-opus").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de exibição amigável para a interface do utilizador.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Provedor do modelo (ex: "OpenAI", "Anthropic", "Internal").</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Tipo funcional do modelo — determina os contextos de utilização.</summary>
    public ModelType ModelType { get; private set; }

    /// <summary>Indica se o modelo é interno/local à organização.</summary>
    public bool IsInternal { get; private set; }

    /// <summary>Indica se o modelo é externo (derivado de !IsInternal).</summary>
    public bool IsExternal { get; private set; }

    /// <summary>Estado atual do ciclo de vida do modelo.</summary>
    public ModelStatus Status { get; private set; }

    /// <summary>Capacidades do modelo separadas por vírgula (ex: "chat,code,reasoning").</summary>
    public string Capabilities { get; private set; } = string.Empty;

    /// <summary>Casos de uso padrão recomendados para este modelo.</summary>
    public string DefaultUseCases { get; private set; } = string.Empty;

    /// <summary>Nível de sensibilidade (1 = baixo, 5 = máximo) — influencia políticas de acesso.</summary>
    public int SensitivityLevel { get; private set; }

    /// <summary>Data/hora UTC em que o modelo foi registrado no Model Registry.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

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
        DateTimeOffset registeredAt)
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
            DisplayName = displayName,
            Provider = provider,
            ModelType = modelType,
            IsInternal = isInternal,
            IsExternal = !isInternal,
            Status = ModelStatus.Active,
            Capabilities = capabilities,
            DefaultUseCases = string.Empty,
            SensitivityLevel = sensitivityLevel,
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
