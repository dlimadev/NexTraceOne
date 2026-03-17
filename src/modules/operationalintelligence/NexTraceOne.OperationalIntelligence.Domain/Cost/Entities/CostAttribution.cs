using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Entidade que atribui custo a uma API/serviço específico para um determinado período.
/// Permite correlacionar custos de infraestrutura com ativos do catálogo de APIs,
/// calculando o custo por requisição para análise de eficiência.
/// </summary>
public sealed class CostAttribution : AuditableEntity<CostAttributionId>
{
    private CostAttribution() { }

    /// <summary>Identificador do ativo de API no catálogo ao qual o custo é atribuído.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome do serviço responsável pelo custo.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Início do período de atribuição de custo.</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Fim do período de atribuição de custo.</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>Custo total atribuído ao serviço/API neste período.</summary>
    public decimal TotalCost { get; private set; }

    /// <summary>Número de requisições processadas pelo serviço/API neste período.</summary>
    public long RequestCount { get; private set; }

    /// <summary>Custo por requisição, calculado a partir do custo total e contagem de requisições.</summary>
    public decimal CostPerRequest { get; private set; }

    /// <summary>Ambiente onde o custo foi apurado (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>
    /// Cria uma nova atribuição de custo para um serviço/API num período específico.
    /// Valida que o período é consistente (início antes do fim) e que os valores são positivos.
    /// O custo por requisição é calculado automaticamente se houver requisições.
    /// </summary>
    public static Result<CostAttribution> Attribute(
        Guid apiAssetId,
        string serviceName,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        decimal totalCost,
        long requestCount,
        string environment)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);

        if (periodStart >= periodEnd)
            return CostIntelligenceErrors.InvalidPeriod(periodStart, periodEnd);

        if (totalCost < 0)
            return CostIntelligenceErrors.NegativeCost(totalCost);

        Guard.Against.Negative(requestCount);

        var attribution = new CostAttribution
        {
            Id = CostAttributionId.New(),
            ApiAssetId = apiAssetId,
            ServiceName = serviceName,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalCost = totalCost,
            RequestCount = requestCount,
            Environment = environment,
            CostPerRequest = requestCount > 0 ? totalCost / requestCount : 0m
        };

        return attribution;
    }

    /// <summary>
    /// Atualiza os valores de custo e requisições deste período.
    /// Recalcula automaticamente o custo por requisição.
    /// Retorna erro se os novos valores forem inválidos.
    /// </summary>
    public Result<Unit> UpdateCosts(decimal totalCost, long requestCount)
    {
        if (totalCost < 0)
            return CostIntelligenceErrors.NegativeCost(totalCost);

        Guard.Against.Negative(requestCount);

        TotalCost = totalCost;
        RequestCount = requestCount;
        CostPerRequest = requestCount > 0 ? totalCost / requestCount : 0m;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de CostAttribution.</summary>
public sealed record CostAttributionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostAttributionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostAttributionId From(Guid id) => new(id);
}
