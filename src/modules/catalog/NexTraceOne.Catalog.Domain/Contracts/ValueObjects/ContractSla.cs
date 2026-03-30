using Ardalis.GuardClauses;

namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Define os acordos de nível de serviço (SLA/SLO) de uma versão de contrato.
/// Permite que o NexTraceOne associe expectativas de confiabilidade aos contratos,
/// correlacionando-as com dados reais de observabilidade e change intelligence.
/// </summary>
public sealed record ContractSla
{
    private ContractSla() { }

    /// <summary>Disponibilidade alvo em percentagem, ex: 99.9.</summary>
    public decimal? AvailabilityTarget { get; private init; }

    /// <summary>Latência alvo (p99) em milissegundos.</summary>
    public int? LatencyP99Ms { get; private init; }

    /// <summary>Latência alvo (p95) em milissegundos.</summary>
    public int? LatencyP95Ms { get; private init; }

    /// <summary>Taxa de erro máxima aceitável em percentagem, ex: 0.1.</summary>
    public decimal? MaxErrorRatePercent { get; private init; }

    /// <summary>Throughput mínimo garantido em RPS (requests per second).</summary>
    public int? MinThroughputRps { get; private init; }

    /// <summary>Janela de manutenção permitida por mês em minutos.</summary>
    public int? MaintenanceWindowMinutes { get; private init; }

    /// <summary>Classificação do tier de SLA: "Standard", "Premium", "Critical".</summary>
    public string? Tier { get; private init; }

    /// <summary>Referência ao documento formal de SLA (URL ou identificador interno).</summary>
    public string? DocumentReference { get; private init; }

    /// <summary>
    /// Cria um novo SLA para um contrato.
    /// Pelo menos um campo de nível de serviço é obrigatório.
    /// </summary>
    public static ContractSla Create(
        decimal? availabilityTarget = null,
        int? latencyP99Ms = null,
        int? latencyP95Ms = null,
        decimal? maxErrorRatePercent = null,
        int? minThroughputRps = null,
        int? maintenanceWindowMinutes = null,
        string? tier = null,
        string? documentReference = null)
    {
        if (availabilityTarget.HasValue)
            Guard.Against.OutOfRange(availabilityTarget.Value, nameof(availabilityTarget), 0m, 100m);

        if (maxErrorRatePercent.HasValue)
            Guard.Against.OutOfRange(maxErrorRatePercent.Value, nameof(maxErrorRatePercent), 0m, 100m);

        return new ContractSla
        {
            AvailabilityTarget = availabilityTarget,
            LatencyP99Ms = latencyP99Ms,
            LatencyP95Ms = latencyP95Ms,
            MaxErrorRatePercent = maxErrorRatePercent,
            MinThroughputRps = minThroughputRps,
            MaintenanceWindowMinutes = maintenanceWindowMinutes,
            Tier = tier,
            DocumentReference = documentReference,
        };
    }
}
