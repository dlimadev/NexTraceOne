using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Registo de percentagem de rollout de canary deployment para uma release.
/// Permite ao Change Intelligence monitorizar a evolução do rollout e usar
/// a percentagem como factor de confiança — baixo rollout implica menor confiança,
/// rollout completo indica que a release já foi amplamente validada.
/// </summary>
public sealed class CanaryRollout : Entity<CanaryRolloutId>
{
    private CanaryRollout() { }

    /// <summary>Release à qual este registo de canary pertence.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Percentagem actual de rollout (0 a 100).</summary>
    public decimal RolloutPercentage { get; private set; }

    /// <summary>Número de instâncias/pods/targets com a nova versão activa.</summary>
    public int ActiveInstances { get; private set; }

    /// <summary>Número total de instâncias/pods/targets no ambiente.</summary>
    public int TotalInstances { get; private set; }

    /// <summary>Sistema que reportou o estado do canary (ex: Argo Rollouts, Flagger, Split.io).</summary>
    public string SourceSystem { get; private set; } = string.Empty;

    /// <summary>Indica se o canary foi promovido para 100% (rollout completo).</summary>
    public bool IsPromoted { get; private set; }

    /// <summary>Indica se o canary foi revertido (abortado).</summary>
    public bool IsAborted { get; private set; }

    /// <summary>Momento UTC em que este registo de rollout foi criado.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>
    /// Cria um novo registo de canary rollout para uma release.
    /// </summary>
    public static CanaryRollout Create(
        ReleaseId releaseId,
        decimal rolloutPercentage,
        int activeInstances,
        int totalInstances,
        string sourceSystem,
        bool isPromoted,
        bool isAborted,
        DateTimeOffset recordedAt)
    {
        Guard.Against.Default(releaseId);
        Guard.Against.NullOrWhiteSpace(sourceSystem);

        if (rolloutPercentage < 0 || rolloutPercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(rolloutPercentage), "Rollout percentage must be between 0 and 100.");

        if (activeInstances < 0)
            throw new ArgumentOutOfRangeException(nameof(activeInstances), "Active instances cannot be negative.");

        if (totalInstances < 0)
            throw new ArgumentOutOfRangeException(nameof(totalInstances), "Total instances cannot be negative.");

        if (isPromoted && isAborted)
            throw new ArgumentException("A canary rollout cannot be both promoted and aborted simultaneously.");

        return new CanaryRollout
        {
            Id = CanaryRolloutId.New(),
            ReleaseId = releaseId,
            RolloutPercentage = rolloutPercentage,
            ActiveInstances = activeInstances,
            TotalInstances = totalInstances,
            SourceSystem = sourceSystem.Trim(),
            IsPromoted = isPromoted,
            IsAborted = isAborted,
            RecordedAt = recordedAt
        };
    }
}

/// <summary>Identificador fortemente tipado para CanaryRollout.</summary>
public sealed record CanaryRolloutId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static CanaryRolloutId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static CanaryRolloutId From(Guid id) => new(id);
}
