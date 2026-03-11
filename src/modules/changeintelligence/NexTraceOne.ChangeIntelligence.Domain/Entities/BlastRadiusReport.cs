using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ChangeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de BlastRadiusReport.
/// </summary>
public sealed class BlastRadiusReport : AuditableEntity<BlastRadiusReportId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private BlastRadiusReport() { }
}

/// <summary>Identificador fortemente tipado de BlastRadiusReport.</summary>
public sealed record BlastRadiusReportId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static BlastRadiusReportId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static BlastRadiusReportId From(Guid id) => new(id);
}
