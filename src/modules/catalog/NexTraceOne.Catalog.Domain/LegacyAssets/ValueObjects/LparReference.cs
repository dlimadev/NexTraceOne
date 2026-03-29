using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

/// <summary>
/// Referência a uma LPAR (Logical Partition) no mainframe.
/// Identifica a localização física/lógica de um ativo no sysplex.
/// </summary>
public sealed class LparReference : ValueObject
{
    private LparReference() { }

    /// <summary>Nome do sysplex (cluster de LPARs).</summary>
    public string SysplexName { get; private set; } = string.Empty;

    /// <summary>Nome da LPAR específica.</summary>
    public string LparName { get; private set; } = string.Empty;

    /// <summary>Nome da região (CICS, IMS, etc.) quando aplicável.</summary>
    public string RegionName { get; private set; } = string.Empty;

    /// <summary>
    /// Cria uma nova referência LPAR validada.
    /// </summary>
    public static LparReference Create(string sysplexName, string lparName, string? regionName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sysplexName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lparName);

        return new LparReference
        {
            SysplexName = sysplexName.Trim(),
            LparName = lparName.Trim(),
            RegionName = regionName?.Trim() ?? string.Empty
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SysplexName;
        yield return LparName;
        yield return RegionName;
    }
}
