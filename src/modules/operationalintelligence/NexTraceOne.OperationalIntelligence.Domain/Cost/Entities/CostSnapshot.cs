using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Domain.Errors;

namespace NexTraceOne.CostIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um snapshot pontual de custo de infraestrutura de um serviço.
/// Captura os custos totais e a distribuição por componente (CPU, memória, rede, storage)
/// num instante específico, permitindo análise histórica e detecção de anomalias.
/// </summary>
public sealed class CostSnapshot : AuditableEntity<CostSnapshotId>
{
    private CostSnapshot() { }

    /// <summary>Nome do serviço ao qual este snapshot de custo se refere.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente onde o custo foi mensurado (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Custo total do serviço no período capturado.</summary>
    public decimal TotalCost { get; private set; }

    /// <summary>Parcela do custo total atribuída a CPU.</summary>
    public decimal CpuCostShare { get; private set; }

    /// <summary>Parcela do custo total atribuída a memória.</summary>
    public decimal MemoryCostShare { get; private set; }

    /// <summary>Parcela do custo total atribuída a rede.</summary>
    public decimal NetworkCostShare { get; private set; }

    /// <summary>Parcela do custo total atribuída a storage.</summary>
    public decimal StorageCostShare { get; private set; }

    /// <summary>Moeda do custo (padrão: USD).</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>Data/hora UTC em que o snapshot foi capturado na fonte original.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>Fonte dos dados de custo (ex: "CloudWatch", "Prometheus").</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Granularidade do período capturado: "hourly", "daily" ou "monthly".</summary>
    public string Period { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo snapshot de custo com validações de guarda nos campos obrigatórios.
    /// A validação das shares vs custo total é feita sob demanda via <see cref="Validate"/>.
    /// </summary>
    public static CostSnapshot Create(
        string serviceName,
        string environment,
        decimal totalCost,
        decimal cpuCostShare,
        decimal memoryCostShare,
        decimal networkCostShare,
        decimal storageCostShare,
        DateTimeOffset capturedAt,
        string source,
        string period,
        string currency = "USD")
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.Negative(totalCost);
        Guard.Against.Negative(cpuCostShare);
        Guard.Against.Negative(memoryCostShare);
        Guard.Against.Negative(networkCostShare);
        Guard.Against.Negative(storageCostShare);
        Guard.Against.NullOrWhiteSpace(source);
        Guard.Against.NullOrWhiteSpace(period);
        Guard.Against.NullOrWhiteSpace(currency);

        return new CostSnapshot
        {
            Id = CostSnapshotId.New(),
            ServiceName = serviceName,
            Environment = environment,
            TotalCost = totalCost,
            CpuCostShare = cpuCostShare,
            MemoryCostShare = memoryCostShare,
            NetworkCostShare = networkCostShare,
            StorageCostShare = storageCostShare,
            Currency = currency,
            CapturedAt = capturedAt,
            Source = source,
            Period = period
        };
    }

    /// <summary>
    /// Valida que a soma das parcelas de custo (CPU + memória + rede + storage)
    /// não excede o custo total informado. Retorna erro de validação se exceder.
    /// </summary>
    public Result<Unit> Validate()
    {
        var sharesSum = CpuCostShare + MemoryCostShare + NetworkCostShare + StorageCostShare;

        if (sharesSum > TotalCost)
            return CostIntelligenceErrors.InvalidCostShares(sharesSum, TotalCost);

        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de CostSnapshot.</summary>
public sealed record CostSnapshotId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostSnapshotId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostSnapshotId From(Guid id) => new(id);
}
