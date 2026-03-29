using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Campo de um copybook COBOL — representa um campo individual no layout de dados.
/// Armazena informação de PIC clause, offset e tamanho.
/// </summary>
public sealed class CopybookField : Entity<CopybookFieldId>
{
    private CopybookField() { }

    // ── Relação ───────────────────────────────────────────────────────

    /// <summary>Copybook ao qual este campo pertence.</summary>
    public CopybookId CopybookId { get; private set; } = null!;

    // ── Estrutura do campo ────────────────────────────────────────────

    /// <summary>Nome do campo COBOL.</summary>
    public string FieldName { get; private set; } = string.Empty;

    /// <summary>Nível COBOL do campo (01, 02, 05, etc.).</summary>
    public int Level { get; private set; }

    /// <summary>Cláusula PIC do campo (ex.: PIC X(10), PIC 9(5)V99).</summary>
    public string PicClause { get; private set; } = string.Empty;

    /// <summary>Offset do campo no registo (em bytes).</summary>
    public int Offset { get; private set; }

    /// <summary>Tamanho do campo (em bytes).</summary>
    public int Length { get; private set; }

    /// <summary>Tipo de dados inferido (alphanumeric, numeric, packed, etc.).</summary>
    public string DataType { get; private set; } = string.Empty;

    /// <summary>Indica se o campo é um REDEFINES de outro campo.</summary>
    public bool IsRedefines { get; private set; }

    /// <summary>Nome do campo que este campo redefine (quando aplicável).</summary>
    public string? RedefinesField { get; private set; }

    /// <summary>Número de ocorrências OCCURS (quando aplicável).</summary>
    public int? OccursCount { get; private set; }

    /// <summary>Ordem de apresentação do campo no layout.</summary>
    public int SortOrder { get; private set; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria um novo campo de copybook com os campos obrigatórios.</summary>
    public static CopybookField Create(
        CopybookId copybookId, string fieldName, int level,
        string picClause, int offset, int length, int sortOrder)
    {
        Guard.Against.Null(copybookId);
        Guard.Against.NullOrWhiteSpace(fieldName);
        Guard.Against.Negative(level);
        Guard.Against.Negative(offset);
        Guard.Against.Negative(length);

        return new CopybookField
        {
            Id = CopybookFieldId.New(),
            CopybookId = copybookId,
            FieldName = fieldName.Trim(),
            Level = level,
            PicClause = picClause?.Trim() ?? string.Empty,
            Offset = offset,
            Length = length,
            SortOrder = sortOrder
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Define o tipo de dados inferido do campo.</summary>
    public void SetDataType(string dataType)
    {
        DataType = dataType ?? string.Empty;
    }

    /// <summary>Marca o campo como REDEFINES de outro campo.</summary>
    public void SetRedefines(string redefinesField)
    {
        IsRedefines = true;
        RedefinesField = redefinesField;
    }

    /// <summary>Define o número de ocorrências OCCURS.</summary>
    public void SetOccurs(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        OccursCount = count;
    }
}

/// <summary>Identificador fortemente tipado de CopybookField.</summary>
public sealed record CopybookFieldId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CopybookFieldId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CopybookFieldId From(Guid id) => new(id);
}
