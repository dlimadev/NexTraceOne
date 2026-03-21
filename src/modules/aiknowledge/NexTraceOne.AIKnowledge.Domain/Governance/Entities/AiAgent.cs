using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa um Agent de IA registado na plataforma NexTraceOne.
/// Agents são capacidades especializadas disponíveis no chat, cada um com
/// categoria, modelo preferido, prompt de sistema e políticas de acesso.
///
/// Ownership: System (plataforma), Tenant (organização), User (individual).
/// Visibilidade: Private, Team, Tenant.
/// Ciclo de vida: Draft → PendingReview → Active → Published → Archived | Blocked.
///
/// Invariantes:
/// - Nome e DisplayName são obrigatórios.
/// - Slug é derivado automaticamente do Name se não fornecido.
/// - System agents iniciam Active; User/Tenant agents iniciam Draft.
/// - IsOfficial é derivado: true se OwnershipType == System.
/// - AllowedModelIds restringe modelos utilizáveis pelo agent.
/// - AllowedTools restringe tools utilizáveis (catálogo aprovado).
/// </summary>
public sealed class AiAgent : AuditableEntity<AiAgentId>
{
    private AiAgent() { }

    /// <summary>Nome técnico do agent (único, lowercase, sem espaços).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do agent na UI.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Slug derivado do nome para URLs e referências.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito e capacidades do agent.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Categoria funcional do agent.</summary>
    public AgentCategory Category { get; private set; }

    /// <summary>Indica se é um agent oficial da plataforma (derivado de OwnershipType == System).</summary>
    public bool IsOfficial { get; private set; }

    /// <summary>Indica se o agent está disponível para uso.</summary>
    public bool IsActive { get; private set; }

    /// <summary>System prompt base utilizado pelo agent.</summary>
    public string SystemPrompt { get; private set; } = string.Empty;

    /// <summary>Identificador do modelo default do agent (opcional).</summary>
    public Guid? PreferredModelId { get; private set; }

    /// <summary>Capacidades do agent separadas por vírgula (ex: "chat,analysis,generation").</summary>
    public string Capabilities { get; private set; } = string.Empty;

    /// <summary>Persona de destino principal do agent (ex: "Engineer", "Architect").</summary>
    public string TargetPersona { get; private set; } = string.Empty;

    /// <summary>Ícone ou emoji representativo do agent.</summary>
    public string Icon { get; private set; } = string.Empty;

    /// <summary>Prioridade de ordenação (menor = mais alta prioridade).</summary>
    public int SortOrder { get; private set; }

    // ── Phase 3: Agent Runtime Foundation ────────────────────────────────

    /// <summary>Tipo de ownership: System, Tenant ou User.</summary>
    public AgentOwnershipType OwnershipType { get; private set; }

    /// <summary>Visibilidade do agent: Private, Team ou Tenant.</summary>
    public AgentVisibility Visibility { get; private set; }

    /// <summary>Estado de publicação do agent.</summary>
    public AgentPublicationStatus PublicationStatus { get; private set; }

    /// <summary>Identificador do proprietário (userId ou tenantId).</summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>Identificador da equipa proprietária (para visibilidade Team).</summary>
    public string OwnerTeamId { get; private set; } = string.Empty;

    /// <summary>IDs de modelos permitidos para este agent separados por vírgula. Vazio = sem restrição.</summary>
    public string AllowedModelIds { get; private set; } = string.Empty;

    /// <summary>Tools aprovadas para este agent separadas por vírgula. Vazio = sem tools.</summary>
    public string AllowedTools { get; private set; } = string.Empty;

    /// <summary>Objectivo/instrução de alto nível do agent (complementa SystemPrompt).</summary>
    public string Objective { get; private set; } = string.Empty;

    /// <summary>Formato de entrada esperado (JSON schema ou descrição).</summary>
    public string InputSchema { get; private set; } = string.Empty;

    /// <summary>Formato de saída esperado (JSON schema ou descrição).</summary>
    public string OutputSchema { get; private set; } = string.Empty;

    /// <summary>Permite override de modelo na execução.</summary>
    public bool AllowModelOverride { get; private set; }

    /// <summary>Número de versão do agent (incrementa em cada publicação).</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Total de execuções do agent.</summary>
    public long ExecutionCount { get; private set; }

    /// <summary>
    /// Regista um agent oficial da plataforma (System).
    /// Agents System iniciam Active e Published imediatamente.
    /// </summary>
    public static AiAgent Register(
        string name,
        string displayName,
        string description,
        AgentCategory category,
        bool isOfficial,
        string systemPrompt,
        string? slug = null,
        Guid? preferredModelId = null,
        string? capabilities = null,
        string? targetPersona = null,
        string? icon = null,
        int sortOrder = 100)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);

        var derivedSlug = slug ?? name.ToLowerInvariant().Replace(' ', '-').Replace(':', '-');

        return new AiAgent
        {
            Id = AiAgentId.New(),
            Name = name,
            DisplayName = displayName,
            Slug = derivedSlug,
            Description = description ?? string.Empty,
            Category = category,
            IsOfficial = isOfficial,
            IsActive = true,
            SystemPrompt = systemPrompt ?? string.Empty,
            PreferredModelId = preferredModelId,
            Capabilities = capabilities ?? string.Empty,
            TargetPersona = targetPersona ?? string.Empty,
            Icon = icon ?? string.Empty,
            SortOrder = sortOrder,
            OwnershipType = AgentOwnershipType.System,
            Visibility = AgentVisibility.Tenant,
            PublicationStatus = AgentPublicationStatus.Published,
            OwnerId = "system",
            AllowModelOverride = true,
            Version = 1,
        };
    }

    /// <summary>
    /// Cria um agent customizado por um utilizador ou organização.
    /// Agents User/Tenant iniciam em Draft e precisam ser activados.
    /// </summary>
    public static AiAgent CreateCustom(
        string name,
        string displayName,
        string description,
        AgentCategory category,
        string systemPrompt,
        string objective,
        AgentOwnershipType ownershipType,
        AgentVisibility visibility,
        string ownerId,
        string? ownerTeamId = null,
        string? slug = null,
        Guid? preferredModelId = null,
        string? allowedModelIds = null,
        string? allowedTools = null,
        string? capabilities = null,
        string? targetPersona = null,
        string? inputSchema = null,
        string? outputSchema = null,
        string? icon = null,
        bool allowModelOverride = true,
        int sortOrder = 100)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(ownerId);

        if (ownershipType == AgentOwnershipType.System)
            throw new ArgumentException("Use Register() for System agents.", nameof(ownershipType));

        var derivedSlug = slug ?? name.ToLowerInvariant().Replace(' ', '-').Replace(':', '-');

        return new AiAgent
        {
            Id = AiAgentId.New(),
            Name = name,
            DisplayName = displayName,
            Slug = derivedSlug,
            Description = description ?? string.Empty,
            Category = category,
            IsOfficial = false,
            IsActive = false,
            SystemPrompt = systemPrompt ?? string.Empty,
            Objective = objective ?? string.Empty,
            PreferredModelId = preferredModelId,
            Capabilities = capabilities ?? string.Empty,
            TargetPersona = targetPersona ?? string.Empty,
            Icon = icon ?? string.Empty,
            SortOrder = sortOrder,
            OwnershipType = ownershipType,
            Visibility = visibility,
            PublicationStatus = AgentPublicationStatus.Draft,
            OwnerId = ownerId,
            OwnerTeamId = ownerTeamId ?? string.Empty,
            AllowedModelIds = allowedModelIds ?? string.Empty,
            AllowedTools = allowedTools ?? string.Empty,
            InputSchema = inputSchema ?? string.Empty,
            OutputSchema = outputSchema ?? string.Empty,
            AllowModelOverride = allowModelOverride,
            Version = 1,
        };
    }

    /// <summary>Atualiza metadados do agent.</summary>
    public Result<Unit> Update(
        string displayName,
        string description,
        string? capabilities,
        string? targetPersona,
        string? icon,
        int? sortOrder)
    {
        Guard.Against.NullOrWhiteSpace(displayName);

        DisplayName = displayName;
        Description = description ?? string.Empty;
        if (capabilities is not null) Capabilities = capabilities;
        if (targetPersona is not null) TargetPersona = targetPersona;
        if (icon is not null) Icon = icon;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        return Unit.Value;
    }

    /// <summary>Atualiza definição completa de agent customizado.</summary>
    public Result<Unit> UpdateDefinition(
        string displayName,
        string description,
        string? systemPrompt,
        string? objective,
        string? capabilities,
        string? targetPersona,
        string? icon,
        Guid? preferredModelId,
        string? allowedModelIds,
        string? allowedTools,
        string? inputSchema,
        string? outputSchema,
        AgentVisibility? visibility,
        bool? allowModelOverride,
        int? sortOrder)
    {
        Guard.Against.NullOrWhiteSpace(displayName);

        if (OwnershipType == AgentOwnershipType.System)
            return Error.Business("AiGovernance.Agent.SystemAgentImmutable",
                "System agents cannot be modified via user API.");

        DisplayName = displayName;
        Description = description ?? string.Empty;
        if (systemPrompt is not null) SystemPrompt = systemPrompt;
        if (objective is not null) Objective = objective;
        if (capabilities is not null) Capabilities = capabilities;
        if (targetPersona is not null) TargetPersona = targetPersona;
        if (icon is not null) Icon = icon;
        if (preferredModelId.HasValue) PreferredModelId = preferredModelId;
        if (allowedModelIds is not null) AllowedModelIds = allowedModelIds;
        if (allowedTools is not null) AllowedTools = allowedTools;
        if (inputSchema is not null) InputSchema = inputSchema;
        if (outputSchema is not null) OutputSchema = outputSchema;
        if (visibility.HasValue) Visibility = visibility.Value;
        if (allowModelOverride.HasValue) AllowModelOverride = allowModelOverride.Value;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;

        return Unit.Value;
    }

    /// <summary>Ativa o agent para uso.</summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        if (PublicationStatus == AgentPublicationStatus.Draft)
            PublicationStatus = AgentPublicationStatus.Active;
        return Unit.Value;
    }

    /// <summary>Desativa o agent.</summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>Publica o agent para visibilidade alargada.</summary>
    public Result<Unit> Publish()
    {
        if (PublicationStatus == AgentPublicationStatus.Blocked)
            return Error.Business("AiGovernance.Agent.Blocked",
                "Blocked agents cannot be published.");

        PublicationStatus = AgentPublicationStatus.Published;
        IsActive = true;
        Version++;
        return Unit.Value;
    }

    /// <summary>Arquiva o agent.</summary>
    public Result<Unit> Archive()
    {
        PublicationStatus = AgentPublicationStatus.Archived;
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>Bloqueia o agent por governança.</summary>
    public Result<Unit> Block()
    {
        PublicationStatus = AgentPublicationStatus.Blocked;
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>Submete para revisão.</summary>
    public Result<Unit> SubmitForReview()
    {
        if (PublicationStatus != AgentPublicationStatus.Draft &&
            PublicationStatus != AgentPublicationStatus.Active)
            return Error.Business("AiGovernance.Agent.InvalidStatusForReview",
                "Only Draft or Active agents can be submitted for review.");

        PublicationStatus = AgentPublicationStatus.PendingReview;
        return Unit.Value;
    }

    /// <summary>Incrementa o contador de execuções.</summary>
    public void IncrementExecutionCount() => ExecutionCount++;

    /// <summary>Verifica se um modelo específico é permitido para este agent.</summary>
    public bool IsModelAllowed(Guid modelId)
    {
        if (string.IsNullOrWhiteSpace(AllowedModelIds))
            return true;

        return AllowedModelIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(id => Guid.TryParse(id, out var parsed) && parsed == modelId);
    }

    /// <summary>Verifica se o utilizador tem acesso com base na visibilidade e ownership.</summary>
    public bool IsAccessibleBy(string userId, string? teamId)
    {
        if (OwnershipType == AgentOwnershipType.System)
            return true;

        if (Visibility == AgentVisibility.Tenant)
            return true;

        if (Visibility == AgentVisibility.Private)
            return string.Equals(OwnerId, userId, StringComparison.OrdinalIgnoreCase);

        if (Visibility == AgentVisibility.Team && teamId is not null)
            return string.Equals(OwnerTeamId, teamId, StringComparison.OrdinalIgnoreCase);

        return false;
    }
}

/// <summary>Identificador fortemente tipado de AiAgent.</summary>
public sealed record AiAgentId(Guid Value) : TypedIdBase(Value)
{
    public static AiAgentId New() => new(Guid.NewGuid());
    public static AiAgentId From(Guid id) => new(id);
}
