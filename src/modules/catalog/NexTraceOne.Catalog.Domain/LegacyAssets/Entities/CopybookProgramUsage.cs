using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Relação entre programa COBOL e copybook — qual programa usa qual copybook.
/// Permite rastreio de impacto de alterações de copybook.
/// </summary>
public sealed class CopybookProgramUsage : Entity<CopybookProgramUsageId>
{
    private CopybookProgramUsage() { }

    // ── Relação ───────────────────────────────────────────────────────

    /// <summary>Programa COBOL que utiliza o copybook.</summary>
    public CobolProgramId ProgramId { get; private set; } = null!;

    /// <summary>Copybook utilizado pelo programa.</summary>
    public CopybookId CopybookId { get; private set; } = null!;

    /// <summary>Tipo de uso (ex.: COPY, REPLACE).</summary>
    public string UsageType { get; private set; } = string.Empty;

    // ── Auditoria ─────────────────────────────────────────────────────

    /// <summary>Data de descoberta da relação.</summary>
    public DateTimeOffset DiscoveredAt { get; private init; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria uma nova relação de uso entre programa e copybook.</summary>
    public static CopybookProgramUsage Create(
        CobolProgramId programId, CopybookId copybookId, string? usageType = null)
    {
        Guard.Against.Null(programId);
        Guard.Against.Null(copybookId);

        return new CopybookProgramUsage
        {
            Id = CopybookProgramUsageId.New(),
            ProgramId = programId,
            CopybookId = copybookId,
            UsageType = usageType?.Trim() ?? "COPY",
            DiscoveredAt = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>Identificador fortemente tipado de CopybookProgramUsage.</summary>
public sealed record CopybookProgramUsageId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CopybookProgramUsageId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CopybookProgramUsageId From(Guid id) => new(id);
}
