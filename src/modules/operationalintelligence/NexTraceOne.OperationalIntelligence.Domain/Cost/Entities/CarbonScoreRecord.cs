using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>Identificador fortemente tipado de CarbonScoreRecord.</summary>
public sealed record CarbonScoreRecordId(Guid Value) : TypedIdBase(Value)
{
    public static CarbonScoreRecordId New() => new(Guid.NewGuid());
    public static CarbonScoreRecordId From(Guid id) => new(id);
}

/// <summary>
/// Registo de emissão de carbono por serviço, calculado diariamente.
/// Fórmula: CpuHours × IntensityFactor + MemoryGbHours × 0.392 + NetworkGb × 60
/// IntensityFactor padrão: 233 gCO₂/kWh (média europeia 2026).
/// W6-04: GreenOps / Carbon Score.
/// </summary>
public sealed class CarbonScoreRecord : AuditableEntity<CarbonScoreRecordId>
{
    private CarbonScoreRecord() { }

    /// <summary>Identificador do serviço.</summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Data do cálculo (UTC, apenas data).</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Horas de CPU consumidas no período.</summary>
    public double CpuHours { get; private set; }

    /// <summary>Horas de memória em GB consumidas no período.</summary>
    public double MemoryGbHours { get; private set; }

    /// <summary>Gigabytes de rede transferidos.</summary>
    public double NetworkGb { get; private set; }

    /// <summary>Gramas de CO₂ emitidas (calculado).</summary>
    public double CarbonGrams { get; private set; }

    /// <summary>Factor de intensidade de carbono usado (gCO₂/kWh).</summary>
    public double IntensityFactor { get; private set; }

    public static CarbonScoreRecord Create(
        Guid serviceId,
        Guid tenantId,
        DateOnly date,
        double cpuHours,
        double memoryGbHours,
        double networkGb,
        double intensityFactor)
    {
        Guard.Against.Default(serviceId);
        Guard.Against.Default(tenantId);
        Guard.Against.NegativeOrZero(intensityFactor);

        // Fórmula: CpuHours × intensity + MemGbHours × 0.392 + NetworkGb × 60
        var carbonGrams = cpuHours * intensityFactor + memoryGbHours * 0.392 + networkGb * 60.0;

        return new CarbonScoreRecord
        {
            Id = CarbonScoreRecordId.New(),
            ServiceId = serviceId,
            TenantId = tenantId,
            Date = date,
            CpuHours = cpuHours,
            MemoryGbHours = memoryGbHours,
            NetworkGb = networkGb,
            CarbonGrams = carbonGrams,
            IntensityFactor = intensityFactor,
        };
    }
}
