using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>Tipo de cenário para simulação de impacto em dependências.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImpactSimulationScenario
{
    /// <summary>Remoção de endpoint — simula impacto da remoção de um endpoint existente.</summary>
    EndpointRemoval = 0,

    /// <summary>Indisponibilidade de serviço — simula impacto se o serviço ficar indisponível.</summary>
    ServiceUnavailability = 1,

    /// <summary>Migração de contrato — simula impacto de migração para uma nova versão de contrato.</summary>
    ContractMigration = 2,

    /// <summary>Alteração de schema — simula impacto de mudança em schemas/modelos partilhados.</summary>
    SchemaChange = 3
}
