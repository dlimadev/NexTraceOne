using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Skill de IA registada na plataforma NexTraceOne.
/// Cada skill representa uma capacidade especializada encapsulada em SKILL.md,
/// com tools obrigatórias, modelos preferidos e esquemas de entrada/saída.
///
/// Ownership: System (plataforma), Tenant, Team, User, Community.
/// Visibilidade: Public, TeamOnly, Private.
/// Ciclo de vida: Draft → Active → Deprecated.
///
/// Invariantes:
/// - Name, DisplayName e Description são obrigatórios.
/// - SkillContent é o conteúdo SKILL.md da skill.
/// - Skills System iniciam Active; outras iniciam Draft.
/// </summary>
public sealed class AiSkill : AuditableEntity<AiSkillId>
{
    private AiSkill() { }

    /// <summary>Nome técnico único da skill (ex: "incident-triage").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação da skill na UI.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito e capacidades da skill.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Conteúdo SKILL.md — instrução especializada da skill.</summary>
    public string SkillContent { get; private set; } = string.Empty;

    /// <summary>Versão semântica da skill (ex: "1.0.0").</summary>
    public string Version { get; private set; } = "1.0.0";

    /// <summary>Tipo de ownership da skill.</summary>
    public SkillOwnershipType OwnershipType { get; private set; }

    /// <summary>Visibilidade da skill.</summary>
    public SkillVisibility Visibility { get; private set; }

    /// <summary>Estado de ciclo de vida da skill.</summary>
    public SkillStatus Status { get; private set; }

    /// <summary>Tags associadas, armazenadas como CSV (ex: "ops,incident,triage").</summary>
    public string Tags { get; private set; } = string.Empty;

    /// <summary>Tools obrigatórias para execução, armazenadas como CSV.</summary>
    public string RequiredTools { get; private set; } = string.Empty;

    /// <summary>Modelos preferidos para esta skill, armazenados como CSV.</summary>
    public string PreferredModels { get; private set; } = string.Empty;

    /// <summary>JSON Schema de entrada esperada.</summary>
    public string InputSchema { get; private set; } = string.Empty;

    /// <summary>JSON Schema de saída esperada.</summary>
    public string OutputSchema { get; private set; } = string.Empty;

    /// <summary>Contagem total de execuções desta skill.</summary>
    public long ExecutionCount { get; private set; }

    /// <summary>Classificação média com base em feedback (0.0 quando sem feedback).</summary>
    public double AverageRating { get; private set; }

    /// <summary>Identificador do agent pai (para skills compostas).</summary>
    public string? ParentAgentId { get; private set; }

    /// <summary>Indica se a skill pode ser composta com outras skills.</summary>
    public bool IsComposable { get; private set; }

    /// <summary>Identificador do proprietário (userId ou "system").</summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>Identificador da equipa proprietária.</summary>
    public string OwnerTeamId { get; private set; } = string.Empty;

    /// <summary>Tenant ao qual a skill pertence.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Optimistic concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Cria uma skill oficial da plataforma (System).
    /// Skills System iniciam Active e estão imediatamente disponíveis.
    /// </summary>
    public static AiSkill CreateSystem(
        string name,
        string displayName,
        string description,
        string skillContent,
        string[]? tags = null,
        string[]? requiredTools = null,
        string[]? preferredModels = null,
        string? inputSchema = null,
        string? outputSchema = null,
        bool isComposable = false)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);

        return new AiSkill
        {
            Id = AiSkillId.New(),
            Name = name,
            DisplayName = displayName,
            Description = description ?? string.Empty,
            SkillContent = skillContent ?? string.Empty,
            Version = "1.0.0",
            OwnershipType = SkillOwnershipType.System,
            Visibility = SkillVisibility.Public,
            Status = SkillStatus.Active,
            Tags = tags is { Length: > 0 } ? string.Join(",", tags) : string.Empty,
            RequiredTools = requiredTools is { Length: > 0 } ? string.Join(",", requiredTools) : string.Empty,
            PreferredModels = preferredModels is { Length: > 0 } ? string.Join(",", preferredModels) : string.Empty,
            InputSchema = inputSchema ?? string.Empty,
            OutputSchema = outputSchema ?? string.Empty,
            IsComposable = isComposable,
            OwnerId = "system",
            OwnerTeamId = string.Empty,
            TenantId = Guid.Empty,
        };
    }

    /// <summary>
    /// Cria uma skill customizada (Tenant, Team, User ou Community).
    /// Skills customizadas iniciam em Draft.
    /// </summary>
    public static AiSkill CreateCustom(
        string name,
        string displayName,
        string description,
        string skillContent,
        SkillOwnershipType ownershipType,
        SkillVisibility visibility,
        string ownerId,
        Guid tenantId,
        string[]? tags = null,
        string[]? requiredTools = null,
        string[]? preferredModels = null,
        string? inputSchema = null,
        string? outputSchema = null,
        bool isComposable = false,
        string? parentAgentId = null,
        string? ownerTeamId = null)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(ownerId);

        if (ownershipType == SkillOwnershipType.System)
            throw new ArgumentException("Use CreateSystem() for System skills.", nameof(ownershipType));

        return new AiSkill
        {
            Id = AiSkillId.New(),
            Name = name,
            DisplayName = displayName,
            Description = description ?? string.Empty,
            SkillContent = skillContent ?? string.Empty,
            Version = "1.0.0",
            OwnershipType = ownershipType,
            Visibility = visibility,
            Status = SkillStatus.Draft,
            Tags = tags is { Length: > 0 } ? string.Join(",", tags) : string.Empty,
            RequiredTools = requiredTools is { Length: > 0 } ? string.Join(",", requiredTools) : string.Empty,
            PreferredModels = preferredModels is { Length: > 0 } ? string.Join(",", preferredModels) : string.Empty,
            InputSchema = inputSchema ?? string.Empty,
            OutputSchema = outputSchema ?? string.Empty,
            IsComposable = isComposable,
            ParentAgentId = parentAgentId,
            OwnerId = ownerId,
            OwnerTeamId = ownerTeamId ?? string.Empty,
            TenantId = tenantId,
        };
    }

    /// <summary>Atualiza os metadados editáveis da skill.</summary>
    public void Update(
        string displayName,
        string description,
        string skillContent,
        string[]? tags,
        string[]? requiredTools,
        string[]? preferredModels,
        string? inputSchema,
        string? outputSchema,
        bool? isComposable)
    {
        Guard.Against.NullOrWhiteSpace(displayName);

        DisplayName = displayName;
        Description = description ?? string.Empty;
        SkillContent = skillContent ?? string.Empty;
        if (tags is not null) Tags = string.Join(",", tags);
        if (requiredTools is not null) RequiredTools = string.Join(",", requiredTools);
        if (preferredModels is not null) PreferredModels = string.Join(",", preferredModels);
        if (inputSchema is not null) InputSchema = inputSchema;
        if (outputSchema is not null) OutputSchema = outputSchema;
        if (isComposable.HasValue) IsComposable = isComposable.Value;
    }

    /// <summary>Ativa a skill para uso.</summary>
    public void Activate() => Status = SkillStatus.Active;

    /// <summary>Descontinua a skill.</summary>
    public void Deprecate() => Status = SkillStatus.Deprecated;

    /// <summary>Publica a skill (equivalente a Activate com bump de versão).</summary>
    public void Publish()
    {
        Status = SkillStatus.Active;
        var parts = Version.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[2], out var patch))
            Version = $"{parts[0]}.{parts[1]}.{patch + 1}";
    }

    /// <summary>Incrementa o contador de execuções.</summary>
    public void IncrementExecutionCount() => ExecutionCount++;

    /// <summary>Atualiza a classificação média com base no novo rating submetido.</summary>
    public void UpdateAverageRating(double newRating)
    {
        if (ExecutionCount <= 0)
        {
            AverageRating = newRating;
            return;
        }

        // Weighted moving average
        AverageRating = ((AverageRating * (ExecutionCount - 1)) + newRating) / ExecutionCount;
    }
}

/// <summary>Identificador fortemente tipado de AiSkill.</summary>
public sealed record AiSkillId(Guid Value) : TypedIdBase(Value)
{
    public static AiSkillId New() => new(Guid.NewGuid());
    public static AiSkillId From(Guid id) => new(id);
}
