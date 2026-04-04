using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Estado dos feature flags activos no momento de uma release.
/// Regista quantas flags estão activas e quantas são flags de sistema crítico,
/// permitindo avaliar o risco da mudança com base em feature flag coverage.
/// Entidade usada para enriquecer o advisory de confiança da release.
/// </summary>
public sealed class ReleaseFeatureFlagState : Entity<ReleaseFeatureFlagStateId>
{
    private ReleaseFeatureFlagState() { }

    /// <summary>Release à qual este estado de feature flags pertence.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Total de feature flags activas no momento do deploy.</summary>
    public int ActiveFlagCount { get; private set; }

    /// <summary>Número de feature flags marcadas como críticas (alto impacto).</summary>
    public int CriticalFlagCount { get; private set; }

    /// <summary>Número de feature flags que expõem nova funcionalidade (não apenas kill-switches).</summary>
    public int NewFeatureFlagCount { get; private set; }

    /// <summary>Provider de feature flags utilizado (ex: LaunchDarkly, Unleash, Split.io).</summary>
    public string FlagProvider { get; private set; } = string.Empty;

    /// <summary>Payload JSON opcional com a lista de flags activas para auditoria.</summary>
    public string? FlagsJson { get; private set; }

    /// <summary>Momento UTC em que o estado foi registado.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>
    /// Cria um novo estado de feature flags para uma release.
    /// </summary>
    public static ReleaseFeatureFlagState Create(
        ReleaseId releaseId,
        int activeFlagCount,
        int criticalFlagCount,
        int newFeatureFlagCount,
        string flagProvider,
        string? flagsJson,
        DateTimeOffset recordedAt)
    {
        Guard.Against.Default(releaseId);
        Guard.Against.Negative(activeFlagCount);
        Guard.Against.Negative(criticalFlagCount);
        Guard.Against.Negative(newFeatureFlagCount);
        Guard.Against.NullOrWhiteSpace(flagProvider);

        return new ReleaseFeatureFlagState
        {
            Id = ReleaseFeatureFlagStateId.New(),
            ReleaseId = releaseId,
            ActiveFlagCount = activeFlagCount,
            CriticalFlagCount = criticalFlagCount,
            NewFeatureFlagCount = newFeatureFlagCount,
            FlagProvider = flagProvider.Trim(),
            FlagsJson = flagsJson,
            RecordedAt = recordedAt
        };
    }
}

/// <summary>Identificador fortemente tipado para ReleaseFeatureFlagState.</summary>
public sealed record ReleaseFeatureFlagStateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static ReleaseFeatureFlagStateId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static ReleaseFeatureFlagStateId From(Guid id) => new(id);
}
