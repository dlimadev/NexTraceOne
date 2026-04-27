using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Uma versão imutável de um PromptAsset.
/// Versões nunca são modificadas após criação — alterações geram sempre uma nova versão.
/// </summary>
public sealed class PromptVersion : AuditableEntity<PromptVersionId>
{
    private PromptVersion() { }

    /// <summary>FK para o PromptAsset pai.</summary>
    public PromptAssetId AssetId { get; private set; } = default!;

    /// <summary>Número sequencial da versão (1, 2, 3…).</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Conteúdo completo do prompt desta versão.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Notas de alteração relativas à versão anterior.</summary>
    public string ChangeNotes { get; private set; } = string.Empty;

    /// <summary>Score de avaliação automática ou humana (0.0 – 1.0).</summary>
    public decimal? EvalScore { get; private set; }

    /// <summary>Indica se esta é a versão activa/publicada do asset.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Utilizador que criou esta versão.</summary>
    public new string CreatedBy { get; private set; } = string.Empty;

    internal static PromptVersion Create(
        PromptAssetId assetId,
        int versionNumber,
        string content,
        string changeNotes,
        decimal? evalScore,
        string createdBy)
    {
        Guard.Against.Null(assetId);
        Guard.Against.NegativeOrZero(versionNumber);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(createdBy);

        return new PromptVersion
        {
            Id = PromptVersionId.New(),
            AssetId = assetId,
            VersionNumber = versionNumber,
            Content = content,
            ChangeNotes = changeNotes?.Trim() ?? string.Empty,
            EvalScore = evalScore,
            IsActive = true,
            CreatedBy = createdBy.Trim(),
        };
    }

    public void SetEvalScore(decimal score)
    {
        Guard.Against.OutOfRange(score, nameof(score), 0m, 1m);
        EvalScore = score;
    }
}

/// <summary>Identificador fortemente tipado de PromptVersion.</summary>
public sealed record PromptVersionId(Guid Value) : TypedIdBase(Value)
{
    public static PromptVersionId New() => new(Guid.NewGuid());
    public static PromptVersionId From(Guid id) => new(id);
}
