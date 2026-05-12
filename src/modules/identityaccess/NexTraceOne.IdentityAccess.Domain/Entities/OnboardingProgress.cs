using System.Text.Json;
using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Identificador fortemente tipado de OnboardingProgress.</summary>
public sealed record OnboardingProgressId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static OnboardingProgressId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static OnboardingProgressId From(Guid id) => new(id);
}

/// <summary>
/// Progresso do wizard de onboarding de um tenant.
/// Rastreia quais passos foram concluídos e o passo actual.
/// SaaS-06: Onboarding Wizard.
/// </summary>
public sealed class OnboardingProgress : Entity<OnboardingProgressId>
{
    private OnboardingProgress() { }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Passo actual do wizard.</summary>
    public OnboardingStep CurrentStep { get; private set; } = OnboardingStep.Install;

    /// <summary>
    /// Passos concluídos serializados como JSON (uso interno pelo EF Core).
    /// Utilizar <see cref="CompletedSteps"/> para leitura.
    /// </summary>
    public string CompletedStepsJson { get; private set; } = "[]";

    /// <summary>Passos concluídos até ao momento.</summary>
    public IReadOnlyList<OnboardingStep> CompletedSteps =>
        JsonSerializer.Deserialize<List<string>>(CompletedStepsJson)!
            .Select(s => Enum.Parse<OnboardingStep>(s))
            .ToList()
            .AsReadOnly();

    /// <summary>Timestamp UTC em que o wizard foi ignorado (Skip All).</summary>
    public DateTimeOffset? SkippedAt { get; private set; }

    /// <summary>Timestamp UTC em que o onboarding foi concluído com sucesso.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Indica se o onboarding está concluído.</summary>
    public bool IsCompleted => CompletedAt.HasValue;

    /// <summary>Cria uma nova instância de progresso de onboarding para o tenant indicado.</summary>
    public static OnboardingProgress Create(Guid tenantId)
    {
        Guard.Against.Default(tenantId);
        return new OnboardingProgress
        {
            Id = OnboardingProgressId.New(),
            TenantId = tenantId,
            CurrentStep = OnboardingStep.Install,
            CompletedStepsJson = "[]",
        };
    }

    /// <summary>Avança para o próximo passo e marca o actual como concluído.</summary>
    public void AdvanceStep(OnboardingStep completedStep, DateTimeOffset now)
    {
        // Adiciona o passo concluído à lista, evitando duplicados
        var steps = JsonSerializer.Deserialize<List<string>>(CompletedStepsJson)!;
        var stepName = completedStep.ToString();
        if (!steps.Contains(stepName))
            steps.Add(stepName);
        CompletedStepsJson = JsonSerializer.Serialize(steps);

        var allSteps = Enum.GetValues<OnboardingStep>().OrderBy(s => (int)s).ToList();
        var nextIndex = allSteps.IndexOf(completedStep) + 1;

        if (nextIndex < allSteps.Count)
        {
            CurrentStep = allSteps[nextIndex];
        }
        else
        {
            // Todos os passos concluídos
            CompletedAt = now;
        }
    }

    /// <summary>Marca o wizard como ignorado.</summary>
    public void Skip(DateTimeOffset now) => SkippedAt = now;
}
