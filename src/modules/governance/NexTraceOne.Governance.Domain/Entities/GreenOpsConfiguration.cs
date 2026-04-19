using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para GreenOpsConfiguration.</summary>
public sealed record GreenOpsConfigurationId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Configuração persistida do módulo GreenOps por tenant.
/// Armazena factor de intensidade de emissões e meta ESG.
/// </summary>
public sealed class GreenOpsConfiguration : Entity<GreenOpsConfigurationId>
{
    private GreenOpsConfiguration() { }

    /// <summary>Factor de intensidade de carbono (kg CO₂ por kWh). Típico: 0.233 para EU.</summary>
    public double IntensityFactorKgPerKwh { get; private set; } = 0.233;

    /// <summary>Meta ESG mensal em kg de CO₂.</summary>
    public double EsgTargetKgCo2PerMonth { get; private set; } = 100.0;

    /// <summary>Região do datacenter (ex: "eu-west-1", "us-east-1").</summary>
    public string DatacenterRegion { get; private set; } = "eu-west-1";

    /// <summary>Identificador do tenant.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Cria uma nova configuração GreenOps.</summary>
    public static GreenOpsConfiguration Create(
        double intensityFactorKgPerKwh,
        double esgTargetKgCo2PerMonth,
        string datacenterRegion,
        DateTimeOffset now,
        Guid? tenantId = null)
    {
        Guard.Against.NullOrWhiteSpace(datacenterRegion);
        Guard.Against.OutOfRange(intensityFactorKgPerKwh, nameof(intensityFactorKgPerKwh), 0.001, 2.0);
        Guard.Against.OutOfRange(esgTargetKgCo2PerMonth, nameof(esgTargetKgCo2PerMonth), 0, double.MaxValue);

        return new GreenOpsConfiguration
        {
            Id = new GreenOpsConfigurationId(Guid.NewGuid()),
            IntensityFactorKgPerKwh = intensityFactorKgPerKwh,
            EsgTargetKgCo2PerMonth = esgTargetKgCo2PerMonth,
            DatacenterRegion = datacenterRegion.Trim(),
            TenantId = tenantId,
            UpdatedAt = now
        };
    }

    /// <summary>Atualiza a configuração GreenOps.</summary>
    public void Update(
        double intensityFactorKgPerKwh,
        double esgTargetKgCo2PerMonth,
        string datacenterRegion,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(datacenterRegion);
        Guard.Against.OutOfRange(intensityFactorKgPerKwh, nameof(intensityFactorKgPerKwh), 0.001, 2.0);
        Guard.Against.OutOfRange(esgTargetKgCo2PerMonth, nameof(esgTargetKgCo2PerMonth), 0, double.MaxValue);

        IntensityFactorKgPerKwh = intensityFactorKgPerKwh;
        EsgTargetKgCo2PerMonth = esgTargetKgCo2PerMonth;
        DatacenterRegion = datacenterRegion.Trim();
        UpdatedAt = now;
    }
}
