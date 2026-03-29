using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

/// <summary>
/// Layout resumido de um copybook COBOL.
/// Armazena informações estruturais básicas do layout de dados.
/// O parsing detalhado de campos é feito na entidade CopybookField.
/// </summary>
public sealed class CopybookLayout : ValueObject
{
    private CopybookLayout() { }

    /// <summary>Número total de campos no copybook.</summary>
    public int FieldCount { get; private set; }

    /// <summary>Comprimento total do registo em bytes.</summary>
    public int TotalLength { get; private set; }

    /// <summary>Formato do registo (FB, VB, FBA, VBA, U).</summary>
    public string RecordFormat { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um layout de copybook validado.
    /// </summary>
    public static CopybookLayout Create(int fieldCount, int totalLength, string? recordFormat = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fieldCount);
        ArgumentOutOfRangeException.ThrowIfNegative(totalLength);

        return new CopybookLayout
        {
            FieldCount = fieldCount,
            TotalLength = totalLength,
            RecordFormat = recordFormat?.Trim() ?? string.Empty
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FieldCount;
        yield return TotalLength;
        yield return RecordFormat;
    }
}
