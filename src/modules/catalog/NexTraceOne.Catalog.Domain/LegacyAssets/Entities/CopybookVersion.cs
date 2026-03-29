using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Versão específica de um copybook COBOL — armazena o conteúdo e campos parseados
/// para cada versão, permitindo diff semântico e rastreio de breaking changes.
/// </summary>
public sealed class CopybookVersion : Entity<CopybookVersionId>
{
    private CopybookVersion() { }

    /// <summary>Copybook ao qual esta versão pertence.</summary>
    public CopybookId CopybookId { get; private set; } = null!;

    /// <summary>Label da versão (ex.: "v1.0", "2024-03-29").</summary>
    public string VersionLabel { get; private set; } = string.Empty;

    /// <summary>Conteúdo raw do copybook nesta versão.</summary>
    public string RawContent { get; private set; } = string.Empty;

    /// <summary>Número de campos parseados nesta versão.</summary>
    public int FieldCount { get; private set; }

    /// <summary>Comprimento total do registo em bytes.</summary>
    public int TotalLength { get; private set; }

    /// <summary>Formato do registo (FB, VB, FBA, VBA, U).</summary>
    public string RecordFormat { get; private set; } = string.Empty;

    /// <summary>Data de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Cria uma nova versão de copybook com os campos obrigatórios.</summary>
    public static CopybookVersion Create(
        CopybookId copybookId, string versionLabel, string rawContent,
        int fieldCount, int totalLength, string? recordFormat)
    {
        Guard.Against.Null(copybookId);
        Guard.Against.NullOrWhiteSpace(versionLabel);
        Guard.Against.NullOrWhiteSpace(rawContent);

        return new CopybookVersion
        {
            Id = CopybookVersionId.New(),
            CopybookId = copybookId,
            VersionLabel = versionLabel.Trim(),
            RawContent = rawContent,
            FieldCount = fieldCount,
            TotalLength = totalLength,
            RecordFormat = recordFormat?.Trim() ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>Identificador fortemente tipado de CopybookVersion.</summary>
public sealed record CopybookVersionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CopybookVersionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CopybookVersionId From(Guid id) => new(id);
}
