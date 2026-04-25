using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Identificador fortemente tipado para ContractConsumerInventory.
/// </summary>
public sealed record ContractConsumerInventoryId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Registo de um consumidor real de um contrato derivado de traces OTel.
/// Representa "quem chamou, com que versão, em que ambiente, com que frequência".
///
/// Alimentado pelo ContractConsumerIngestionJob (15 min) que lê traces do Elasticsearch/ClickHouse
/// com http.target matching contratos publicados e persiste/actualiza o inventário via upsert.
///
/// Referência: CC-04.
/// Owner: módulo Catalog (Contracts).
/// </summary>
public sealed class ContractConsumerInventory : Entity<ContractConsumerInventoryId>
{
    /// <summary>Tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Identificador do contrato (ApiAsset) consumido.</summary>
    public Guid ContractId { get; private init; }

    /// <summary>Nome do serviço consumidor (derivado de service.name nos traces).</summary>
    public string ConsumerService { get; private set; } = string.Empty;

    /// <summary>Ambiente em que o consumo foi observado (ex: "production", "staging").</summary>
    public string ConsumerEnvironment { get; private set; } = string.Empty;

    /// <summary>Versão do contrato chamada (derivada de http.target ou headers).</summary>
    public string? Version { get; private set; }

    /// <summary>Frequência de chamadas por dia (média observada no período de lookback).</summary>
    public double FrequencyPerDay { get; private set; }

    /// <summary>Data/hora UTC da última chamada observada.</summary>
    public DateTimeOffset LastCalledAt { get; private set; }

    /// <summary>Data/hora UTC da primeira observação registada.</summary>
    public DateTimeOffset FirstCalledAt { get; private init; }

    /// <summary>Data/hora UTC da última actualização do registo.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private ContractConsumerInventory() { }

    /// <summary>Cria um novo registo de inventário de consumidores.</summary>
    public static ContractConsumerInventory Create(
        string tenantId,
        Guid contractId,
        string consumerService,
        string consumerEnvironment,
        string? version,
        double frequencyPerDay,
        DateTimeOffset calledAt,
        DateTimeOffset utcNow)
    {
        return new ContractConsumerInventory
        {
            Id = new ContractConsumerInventoryId(Guid.NewGuid()),
            TenantId = tenantId,
            ContractId = contractId,
            ConsumerService = consumerService,
            ConsumerEnvironment = consumerEnvironment,
            Version = version,
            FrequencyPerDay = frequencyPerDay,
            LastCalledAt = calledAt,
            FirstCalledAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>Actualiza estatísticas de chamada do consumidor.</summary>
    public void Update(double frequencyPerDay, DateTimeOffset calledAt, DateTimeOffset utcNow)
    {
        FrequencyPerDay = frequencyPerDay;
        if (calledAt > LastCalledAt)
            LastCalledAt = calledAt;
        UpdatedAt = utcNow;
    }
}
