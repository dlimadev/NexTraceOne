using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Sessão de onboarding assistido por IA para novos membros de equipa.
/// Rastreia progresso, recursos explorados e interações com o companion de IA
/// ao longo do processo de integração na plataforma.
/// </summary>
public sealed class OnboardingSession : AuditableEntity<OnboardingSessionId>
{
    private OnboardingSession() { }

    /// <summary>Identificador do utilizador em onboarding.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do utilizador.</summary>
    public string UserDisplayName { get; private set; } = string.Empty;

    /// <summary>Identificador da equipa à qual o utilizador pertence.</summary>
    public Guid TeamId { get; private set; }

    /// <summary>Nome da equipa para consulta rápida.</summary>
    public string TeamName { get; private set; } = string.Empty;

    /// <summary>Nível de experiência declarado pelo utilizador.</summary>
    public OnboardingExperienceLevel ExperienceLevel { get; private set; }

    /// <summary>Estado atual da sessão de onboarding.</summary>
    public OnboardingSessionStatus Status { get; private set; }

    /// <summary>Itens do checklist em formato JSONB.</summary>
    public string ChecklistItems { get; private set; } = string.Empty;

    /// <summary>Número de itens completados.</summary>
    public int CompletedItems { get; private set; }

    /// <summary>Número total de itens no checklist.</summary>
    public int TotalItems { get; private set; }

    /// <summary>Percentagem de progresso (0-100), calculada automaticamente.</summary>
    public int ProgressPercent { get; private set; }

    /// <summary>Lista de serviços explorados em formato JSONB.</summary>
    public string? ServicesExplored { get; private set; }

    /// <summary>Lista de contratos revistos em formato JSONB.</summary>
    public string? ContractsReviewed { get; private set; }

    /// <summary>Lista de runbooks lidos em formato JSONB.</summary>
    public string? RunbooksRead { get; private set; }

    /// <summary>Contagem de interações com o companion de IA.</summary>
    public int AiInteractionCount { get; private set; }

    /// <summary>Timestamp UTC de início da sessão.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Timestamp UTC de conclusão ou abandono da sessão.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Controlo de concorrência optimista.</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova sessão de onboarding para um utilizador.
    /// </summary>
    public static OnboardingSession Create(
        string userId,
        string userDisplayName,
        Guid teamId,
        string teamName,
        OnboardingExperienceLevel experienceLevel,
        string checklistItems,
        int totalItems,
        Guid tenantId,
        DateTimeOffset startedAt)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(userDisplayName);
        Guard.Against.Default(teamId);
        Guard.Against.NullOrWhiteSpace(teamName);
        Guard.Against.EnumOutOfRange(experienceLevel);
        Guard.Against.NullOrWhiteSpace(checklistItems);
        Guard.Against.NegativeOrZero(totalItems);
        Guard.Against.Default(tenantId);

        return new OnboardingSession
        {
            Id = OnboardingSessionId.New(),
            UserId = userId.Trim(),
            UserDisplayName = userDisplayName.Trim(),
            TeamId = teamId,
            TeamName = teamName.Trim(),
            ExperienceLevel = experienceLevel,
            Status = OnboardingSessionStatus.Active,
            ChecklistItems = checklistItems,
            CompletedItems = 0,
            TotalItems = totalItems,
            ProgressPercent = 0,
            AiInteractionCount = 0,
            StartedAt = startedAt,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Atualiza o progresso da sessão de onboarding, recalculando a percentagem.
    /// </summary>
    public void UpdateProgress(
        int completedItems,
        string? servicesExplored,
        string? contractsReviewed,
        string? runbooksRead,
        int aiInteractionCount)
    {
        Guard.Against.Negative(completedItems);
        Guard.Against.Negative(aiInteractionCount);

        CompletedItems = completedItems;
        ServicesExplored = servicesExplored;
        ContractsReviewed = contractsReviewed;
        RunbooksRead = runbooksRead;
        AiInteractionCount = aiInteractionCount;

        ProgressPercent = TotalItems > 0
            ? (int)Math.Round((double)CompletedItems / TotalItems * 100)
            : 0;
    }

    /// <summary>
    /// Marca a sessão como concluída com sucesso.
    /// </summary>
    public void Complete(DateTimeOffset completedAt)
    {
        Status = OnboardingSessionStatus.Completed;
        CompletedAt = completedAt;
    }

    /// <summary>
    /// Marca a sessão como abandonada.
    /// </summary>
    public void Abandon(DateTimeOffset completedAt)
    {
        Status = OnboardingSessionStatus.Abandoned;
        CompletedAt = completedAt;
    }
}

/// <summary>Identificador fortemente tipado de OnboardingSession.</summary>
public sealed record OnboardingSessionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static OnboardingSessionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static OnboardingSessionId From(Guid id) => new(id);
}
