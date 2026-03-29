using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Definição persistida de uma ferramenta (tool) disponível para agentes IA.
/// Substitui o registo exclusivamente em memória por uma representação governada
/// no banco de dados, permitindo gestão administrativa, versionamento e auditoria.
///
/// Cada ferramenta define nome, descrição funcional, categoria, schema de parâmetros
/// (JSON Schema) e metadados de controlo (ativo, requer aprovação, nível de risco).
/// </summary>
public sealed class AiToolDefinition : AuditableEntity<AiToolDefinitionId>
{
    private AiToolDefinition() { }

    /// <summary>Nome técnico único da ferramenta (ex: "list_services", "get_service_health").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de exibição na interface.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição funcional da ferramenta.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Categoria funcional (ex: "service_catalog", "change_governance", "operations").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Schema JSON dos parâmetros aceites pela ferramenta.</summary>
    public string ParametersSchema { get; private set; } = string.Empty;

    /// <summary>Versão da definição da ferramenta.</summary>
    public int Version { get; private set; }

    /// <summary>Indica se a ferramenta está disponível para uso.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Indica se a execução desta ferramenta requer aprovação prévia.</summary>
    public bool RequiresApproval { get; private set; }

    /// <summary>Nível de risco da execução (0=nenhum, 1=baixo, 2=médio, 3=alto).</summary>
    public int RiskLevel { get; private set; }

    /// <summary>Indica se é uma ferramenta oficial da plataforma.</summary>
    public bool IsOfficial { get; private set; }

    /// <summary>Timeout máximo de execução em milissegundos.</summary>
    public int TimeoutMs { get; private set; }

    /// <summary>
    /// Regista uma nova definição de ferramenta.
    /// </summary>
    public static AiToolDefinition Create(
        string name,
        string displayName,
        string description,
        string category,
        string parametersSchema,
        int version,
        bool isActive,
        bool requiresApproval,
        int riskLevel,
        bool isOfficial,
        int timeoutMs = 30000)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.NegativeOrZero(version);
        Guard.Against.OutOfRange(riskLevel, nameof(riskLevel), 0, 3);
        Guard.Against.NegativeOrZero(timeoutMs);

        return new AiToolDefinition
        {
            Id = AiToolDefinitionId.New(),
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Category = category.Trim(),
            ParametersSchema = parametersSchema ?? "{}",
            Version = version,
            IsActive = isActive,
            RequiresApproval = requiresApproval,
            RiskLevel = riskLevel,
            IsOfficial = isOfficial,
            TimeoutMs = timeoutMs
        };
    }

    /// <summary>Desativa a ferramenta.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Ativa a ferramenta.</summary>
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de AiToolDefinition.</summary>
public sealed record AiToolDefinitionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiToolDefinitionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiToolDefinitionId From(Guid id) => new(id);
}
