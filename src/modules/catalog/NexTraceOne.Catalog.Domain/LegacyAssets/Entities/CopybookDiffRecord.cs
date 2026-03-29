using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Resultado persistido de diff semântico entre duas versões de copybook.
/// Permite rastrear breaking changes e impacto entre versões.
/// </summary>
public sealed class CopybookDiffRecord : Entity<CopybookDiffRecordId>
{
    private CopybookDiffRecord() { }

    /// <summary>Copybook ao qual o diff pertence.</summary>
    public CopybookId CopybookId { get; private set; } = null!;

    /// <summary>Versão base (anterior) do diff.</summary>
    public CopybookVersionId BaseVersionId { get; private set; } = null!;

    /// <summary>Versão alvo (mais recente) do diff.</summary>
    public CopybookVersionId TargetVersionId { get; private set; } = null!;

    /// <summary>Nível de mudança global (Breaking, Additive, NonBreaking).</summary>
    public ChangeLevel ChangeLevel { get; private set; }

    /// <summary>Número de breaking changes detectadas.</summary>
    public int BreakingChangeCount { get; private set; }

    /// <summary>Número de additive changes detectadas.</summary>
    public int AdditiveChangeCount { get; private set; }

    /// <summary>Número de non-breaking changes detectadas.</summary>
    public int NonBreakingChangeCount { get; private set; }

    /// <summary>JSON com detalhe das mudanças individuais.</summary>
    public string ChangesJson { get; private set; } = "[]";

    /// <summary>Data em que o diff foi computado.</summary>
    public DateTimeOffset ComputedAt { get; private init; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Cria um novo registo de diff entre versões de copybook.</summary>
    public static CopybookDiffRecord Create(
        CopybookId copybookId,
        CopybookVersionId baseVersionId,
        CopybookVersionId targetVersionId,
        ChangeLevel changeLevel,
        int breakingCount,
        int additiveCount,
        int nonBreakingCount,
        string changesJson)
    {
        Guard.Against.Null(copybookId);
        Guard.Against.Null(baseVersionId);
        Guard.Against.Null(targetVersionId);

        return new CopybookDiffRecord
        {
            Id = CopybookDiffRecordId.New(),
            CopybookId = copybookId,
            BaseVersionId = baseVersionId,
            TargetVersionId = targetVersionId,
            ChangeLevel = changeLevel,
            BreakingChangeCount = breakingCount,
            AdditiveChangeCount = additiveCount,
            NonBreakingChangeCount = nonBreakingCount,
            ChangesJson = changesJson,
            ComputedAt = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>Identificador fortemente tipado de CopybookDiffRecord.</summary>
public sealed record CopybookDiffRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CopybookDiffRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CopybookDiffRecordId From(Guid id) => new(id);
}
