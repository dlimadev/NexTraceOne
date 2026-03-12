using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Entidade que representa o relatório de blast radius de uma release,
/// calculando o total de consumidores diretos e transitivos afetados.
/// </summary>
public sealed class BlastRadiusReport : AuditableEntity<BlastRadiusReportId>
{
    private BlastRadiusReport() { }

    /// <summary>Identificador da release à qual este relatório pertence.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Identificador do ativo de API analisado.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Total de consumidores direta e transitivamente afetados.</summary>
    public int TotalAffectedConsumers { get; private set; }

    /// <summary>Lista de nomes de serviços consumidores diretos.</summary>
    public IReadOnlyList<string> DirectConsumers { get; private set; } = [];

    /// <summary>Lista de nomes de serviços consumidores transitivos.</summary>
    public IReadOnlyList<string> TransitiveConsumers { get; private set; } = [];

    /// <summary>Momento em que o blast radius foi calculado.</summary>
    public DateTimeOffset CalculatedAt { get; private set; }

    /// <summary>
    /// Calcula e cria um relatório de blast radius para a release informada.
    /// </summary>
    public static BlastRadiusReport Calculate(
        ReleaseId releaseId,
        Guid apiAssetId,
        IReadOnlyList<string> directConsumers,
        IReadOnlyList<string> transitiveConsumers,
        DateTimeOffset calculatedAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.Default(apiAssetId);
        Guard.Against.Null(directConsumers);
        Guard.Against.Null(transitiveConsumers);

        return new BlastRadiusReport
        {
            Id = BlastRadiusReportId.New(),
            ReleaseId = releaseId,
            ApiAssetId = apiAssetId,
            DirectConsumers = directConsumers,
            TransitiveConsumers = transitiveConsumers,
            TotalAffectedConsumers = directConsumers.Count + transitiveConsumers.Count,
            CalculatedAt = calculatedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de BlastRadiusReport.</summary>
public sealed record BlastRadiusReportId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static BlastRadiusReportId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static BlastRadiusReportId From(Guid id) => new(id);
}
