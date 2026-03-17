using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Aggregate Root que representa um snapshot pontual de custo de infraestrutura de um serviço.
/// Captura os custos totais e a distribuição por componente (CPU, memória, rede, storage)
/// num instante específico, permitindo análise histórica e detecção de anomalias.
///
/// Invariantes:
/// - Soma das parcelas (CPU + memória + rede + storage) nunca excede o custo total.
/// - Custo total e parcelas são sempre não-negativos.
/// - ServiceName, Environment, Source e Period são obrigatórios.
///
/// REGRA DDD: A validação de invariantes ocorre no factory method — não há estado inválido.
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
    /// Soma das parcelas de custo — propriedade computada, sem persistência.
    /// Facilita verificação de consistência sem recalcular a cada acesso.
    /// </summary>
    public decimal SharesSum => CpuCostShare + MemoryCostShare + NetworkCostShare + StorageCostShare;

    /// <summary>
    /// Indica se há custo não atribuído a nenhum componente (CPU, memória, rede, storage).
    /// Valores positivos indicam custo "invisível" — pode sinalizar omissão na instrumentação.
    /// </summary>
    public decimal UnattributedCost => TotalCost - SharesSum;

    /// <summary>
    /// Factory method para criação de um snapshot de custo validado.
    /// Garante todas as invariantes do aggregate — não existe CostSnapshot em estado inválido.
    /// Retorna Result com erro se a soma das parcelas exceder o custo total.
    /// </summary>
    public static Result<CostSnapshot> Create(
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

        var sharesSum = cpuCostShare + memoryCostShare + networkCostShare + storageCostShare;
        if (sharesSum > totalCost)
            return CostIntelligenceErrors.InvalidCostShares(sharesSum, totalCost);

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
    /// Verifica se o custo atual representa uma anomalia em relação a um custo esperado.
    /// Uma anomalia é detectada quando o custo real ultrapassa o esperado pelo limiar percentual.
    /// Exemplo: com threshold=20 e expectedCost=100, custo acima de 120 é anomalia.
    /// </summary>
    /// <param name="expectedCost">Custo esperado com base em histórico/baseline.</param>
    /// <param name="thresholdPercent">Percentual de desvio tolerado (ex: 20 = ±20%).</param>
    /// <returns>True se o custo total excede o limiar de anomalia.</returns>
    public bool IsAnomaly(decimal expectedCost, decimal thresholdPercent)
    {
        if (expectedCost <= 0m || thresholdPercent <= 0m)
            return false;

        var upperBound = expectedCost * (1m + thresholdPercent / 100m);
        return TotalCost > upperBound;
    }

    /// <summary>
    /// Calcula o percentual de desvio do custo real em relação ao esperado.
    /// Retorna zero quando o custo esperado é zero para evitar divisão por zero.
    /// Valor positivo indica custo acima do esperado, negativo indica abaixo.
    /// </summary>
    public decimal CalculateDeviationPercent(decimal expectedCost)
    {
        if (expectedCost == 0m)
            return TotalCost == 0m ? 0m : 100m;

        return Math.Round((TotalCost - expectedCost) / expectedCost * 100m, 2);
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
