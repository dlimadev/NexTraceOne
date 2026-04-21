using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;

/// <summary>
/// Aggregate Root que representa um registo de alocação de custo operacional por serviço.
///
/// Contextualiza custo por serviço, equipa, domínio e ambiente — base para FinOps Contextual.
/// Permite correlação entre custo e comportamento operacional (mudanças, incidentes, anomalias).
///
/// Cada registo representa a alocação de custo de uma categoria específica
/// para um serviço numa janela temporal (período de faturação ou snapshot).
///
/// Wave I.2 — FinOps Contextual por Serviço (OperationalIntelligence).
/// </summary>
public sealed class ServiceCostAllocationRecord : AuditableEntity<ServiceCostAllocationRecordId>
{
    private const int MaxServiceNameLength = 200;
    private const int MaxEnvironmentLength = 100;
    private const int MaxTeamIdLength = 100;
    private const int MaxDomainNameLength = 200;
    private const int MaxTagsJsonLength = 2000;
    private const int MaxCurrencyLength = 10;
    private const int MaxSourceLength = 100;

    private ServiceCostAllocationRecord() { }

    /// <summary>Identificador do tenant ao qual o registo pertence.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Nome do serviço ao qual o custo é alocado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente de operação (dev, staging, production).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Identificador da equipa responsável pelo serviço.</summary>
    public string? TeamId { get; private set; }

    /// <summary>Domínio de negócio do serviço.</summary>
    public string? DomainName { get; private set; }

    /// <summary>Categoria de custo desta alocação.</summary>
    public CostCategory Category { get; private set; }

    /// <summary>Valor em USD do custo alocado para o período.</summary>
    public decimal AmountUsd { get; private set; }

    /// <summary>Código da moeda original (e.g. EUR, USD, GBP).</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>Valor na moeda original (antes da conversão para USD).</summary>
    public decimal OriginalAmount { get; private set; }

    /// <summary>Início do período de alocação em UTC.</summary>
    public DateTimeOffset PeriodStart { get; private set; }

    /// <summary>Fim do período de alocação em UTC.</summary>
    public DateTimeOffset PeriodEnd { get; private set; }

    /// <summary>Tags adicionais em JSON (para filtragem e agrupamento).</summary>
    public string? TagsJson { get; private set; }

    /// <summary>Fonte de origem do custo (e.g. AWS, Azure, GCP, internal).</summary>
    public string? Source { get; private set; }

    /// <summary>
    /// Cria um novo registo de alocação de custo por serviço.
    /// AmountUsd e OriginalAmount devem ser não-negativos.
    /// PeriodEnd deve ser posterior a PeriodStart.
    /// </summary>
    public static ServiceCostAllocationRecord Create(
        string tenantId,
        string serviceName,
        string environment,
        CostCategory category,
        decimal amountUsd,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset createdAt,
        string? teamId = null,
        string? domainName = null,
        string? currency = null,
        decimal? originalAmount = null,
        string? tagsJson = null,
        string? source = null)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.Negative(amountUsd);
        Guard.Against.InvalidInput(periodEnd, nameof(periodEnd), v => v > periodStart,
            "PeriodEnd must be after PeriodStart.");

        return new ServiceCostAllocationRecord
        {
            Id = ServiceCostAllocationRecordId.New(),
            TenantId = tenantId,
            ServiceName = serviceName[..Math.Min(serviceName.Length, MaxServiceNameLength)],
            Environment = environment[..Math.Min(environment.Length, MaxEnvironmentLength)],
            Category = category,
            AmountUsd = amountUsd,
            Currency = (currency ?? "USD")[..Math.Min((currency ?? "USD").Length, MaxCurrencyLength)],
            OriginalAmount = Math.Max(0, originalAmount ?? amountUsd),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TeamId = teamId is not null ? teamId[..Math.Min(teamId.Length, MaxTeamIdLength)] : null,
            DomainName = domainName is not null ? domainName[..Math.Min(domainName.Length, MaxDomainNameLength)] : null,
            TagsJson = tagsJson is not null ? tagsJson[..Math.Min(tagsJson.Length, MaxTagsJsonLength)] : null,
            Source = source is not null ? source[..Math.Min(source.Length, MaxSourceLength)] : null,
        };
    }

    /// <summary>Corrige o valor em USD após conversão cambial.</summary>
    public void UpdateAmountUsd(decimal amountUsd)
    {
        Guard.Against.Negative(amountUsd);
        AmountUsd = amountUsd;
    }
}

/// <summary>Strongly-typed ID para ServiceCostAllocationRecord.</summary>
public sealed record ServiceCostAllocationRecordId(Guid Value) : TypedIdBase(Value)
{
    public static ServiceCostAllocationRecordId New() => new(Guid.NewGuid());
    public static ServiceCostAllocationRecordId From(Guid id) => new(id);
}
