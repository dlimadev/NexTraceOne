using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

/// <summary>
/// Informação de uma região CICS no mainframe.
/// Encapsula os detalhes de versão e conectividade da região.
/// </summary>
public sealed class CicsRegion : ValueObject
{
    private CicsRegion() { }

    /// <summary>Nome da região CICS.</summary>
    public string RegionName { get; private set; } = string.Empty;

    /// <summary>Versão do CICS Transaction Server.</summary>
    public string CicsVersion { get; private set; } = string.Empty;

    /// <summary>Porta TCP/IP da região (quando aplicável).</summary>
    public int? Port { get; private set; }

    /// <summary>
    /// Cria uma nova referência de região CICS validada.
    /// </summary>
    public static CicsRegion Create(string regionName, string? cicsVersion = null, int? port = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionName);

        return new CicsRegion
        {
            RegionName = regionName.Trim(),
            CicsVersion = cicsVersion?.Trim() ?? string.Empty,
            Port = port
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RegionName;
        yield return CicsVersion;
        yield return Port;
    }
}
