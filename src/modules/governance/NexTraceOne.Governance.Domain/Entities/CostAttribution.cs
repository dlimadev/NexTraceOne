using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade CostAttribution.
/// Garante que nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record CostAttributionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Atribuição de custo operacional a uma dimensão específica (serviço, equipa, domínio, contrato ou mudança).
/// Permite responder "Quanto custa o domínio Payments por mês?" ou "Qual equipa gera mais custo operacional?".
/// Os custos são decompostos em compute, storage, network e outros, com validação de que
/// o total corresponde à soma das parcelas.
/// </summary>
public sealed class CostAttribution : Entity<CostAttributionId>
{
    /// <summary>Dimensão de atribuição (Service, Team, Domain, Contract, Change).</summary>
    public CostAttributionDimension Dimension { get; private init; }

    /// <summary>Chave da entidade específica (nome ou identificador) dentro da dimensão.</summary>
    public string DimensionKey { get; private init; } = string.Empty;

    /// <summary>Label legível para exibição na UI (opcional).</summary>
    public string? DimensionLabel { get; private init; }

    /// <summary>Início do período de atribuição.</summary>
    public DateTimeOffset PeriodStart { get; private init; }

    /// <summary>Fim do período de atribuição.</summary>
    public DateTimeOffset PeriodEnd { get; private init; }

    /// <summary>Custo total atribuído no período (deve ser igual à soma das parcelas).</summary>
    public decimal TotalCost { get; private init; }

    /// <summary>Parcela de custo de computação.</summary>
    public decimal ComputeCost { get; private init; }

    /// <summary>Parcela de custo de armazenamento.</summary>
    public decimal StorageCost { get; private init; }

    /// <summary>Parcela de custo de rede.</summary>
    public decimal NetworkCost { get; private init; }

    /// <summary>Parcela de outros custos.</summary>
    public decimal OtherCost { get; private init; }

    /// <summary>Moeda (padrão: USD).</summary>
    public string Currency { get; private init; } = "USD";

    /// <summary>Detalhamento do custo em formato JSONB.</summary>
    public string? CostBreakdown { get; private init; }

    /// <summary>Método de atribuição utilizado (ex: "telemetry-based", "proportional", "direct").</summary>
    public string? AttributionMethod { get; private init; }

    /// <summary>Fontes de dados utilizadas para o cálculo (JSONB).</summary>
    public string? DataSources { get; private init; }

    /// <summary>Data/hora UTC em que a atribuição foi computada.</summary>
    public DateTimeOffset ComputedAt { get; private init; }

    /// <summary>Identificador do tenant proprietário (nullable para multi-tenant).</summary>
    public string? TenantId { get; private init; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private CostAttribution() { }

    /// <summary>
    /// Cria uma nova atribuição de custo operacional para uma dimensão específica.
    /// Valida que totalCost == computeCost + storageCost + networkCost + otherCost.
    /// </summary>
    /// <param name="dimension">Dimensão de atribuição.</param>
    /// <param name="dimensionKey">Chave da entidade (nome/ID).</param>
    /// <param name="dimensionLabel">Label legível para UI (opcional).</param>
    /// <param name="periodStart">Início do período.</param>
    /// <param name="periodEnd">Fim do período.</param>
    /// <param name="totalCost">Custo total atribuído.</param>
    /// <param name="computeCost">Parcela de compute.</param>
    /// <param name="storageCost">Parcela de storage.</param>
    /// <param name="networkCost">Parcela de network.</param>
    /// <param name="otherCost">Parcela de outros custos.</param>
    /// <param name="currency">Moeda (máx. 10 caracteres).</param>
    /// <param name="costBreakdown">Detalhamento JSONB (opcional).</param>
    /// <param name="attributionMethod">Método de atribuição (opcional).</param>
    /// <param name="dataSources">Fontes de dados JSONB (opcional).</param>
    /// <param name="tenantId">Identificador do tenant (opcional).</param>
    /// <param name="now">Data/hora UTC da computação.</param>
    /// <returns>Nova instância válida de CostAttribution.</returns>
    public static CostAttribution Compute(
        CostAttributionDimension dimension,
        string dimensionKey,
        string? dimensionLabel,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        decimal totalCost,
        decimal computeCost,
        decimal storageCost,
        decimal networkCost,
        decimal otherCost,
        string currency,
        string? costBreakdown,
        string? attributionMethod,
        string? dataSources,
        string? tenantId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(dimensionKey, nameof(dimensionKey));
        Guard.Against.StringTooLong(dimensionKey, 200, nameof(dimensionKey));

        if (dimensionLabel is not null)
            Guard.Against.StringTooLong(dimensionLabel, 300, nameof(dimensionLabel));

        Guard.Against.NullOrWhiteSpace(currency, nameof(currency));
        Guard.Against.StringTooLong(currency, 10, nameof(currency));

        if (attributionMethod is not null)
            Guard.Against.StringTooLong(attributionMethod, 100, nameof(attributionMethod));

        Guard.Against.Negative(totalCost, nameof(totalCost));
        Guard.Against.Negative(computeCost, nameof(computeCost));
        Guard.Against.Negative(storageCost, nameof(storageCost));
        Guard.Against.Negative(networkCost, nameof(networkCost));
        Guard.Against.Negative(otherCost, nameof(otherCost));

        if (periodEnd <= periodStart)
            throw new ArgumentException("Period end must be after period start.", nameof(periodEnd));

        var expectedTotal = computeCost + storageCost + networkCost + otherCost;
        if (totalCost != expectedTotal)
            throw new ArgumentException(
                $"Total cost ({totalCost}) must equal the sum of cost components ({expectedTotal}).",
                nameof(totalCost));

        return new CostAttribution
        {
            Id = new CostAttributionId(Guid.NewGuid()),
            Dimension = dimension,
            DimensionKey = dimensionKey.Trim(),
            DimensionLabel = dimensionLabel?.Trim(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalCost = totalCost,
            ComputeCost = computeCost,
            StorageCost = storageCost,
            NetworkCost = networkCost,
            OtherCost = otherCost,
            Currency = currency.Trim(),
            CostBreakdown = costBreakdown,
            AttributionMethod = attributionMethod?.Trim(),
            DataSources = dataSources,
            TenantId = tenantId?.Trim(),
            ComputedAt = now
        };
    }
}
