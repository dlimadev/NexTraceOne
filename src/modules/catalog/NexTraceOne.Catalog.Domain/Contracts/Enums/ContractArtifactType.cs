using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Tipo de artefato gerado a partir de um contrato.
/// Categoriza os artefatos produzidos pelo módulo, incluindo
/// testes, scaffolds, documentação e evidências regulatórias.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractArtifactType
{
    /// <summary>Artefato de teste de conformidade do provider.</summary>
    ProviderConformanceTest = 0,

    /// <summary>Artefato de contrato para consumer (stubs, mocks).</summary>
    ConsumerContractArtifact = 1,

    /// <summary>Artefato de teste baseado em schema/propriedade.</summary>
    SchemaBasedTest = 2,

    /// <summary>Artefato de validação negativa/segurança.</summary>
    NegativeValidationTest = 3,

    /// <summary>Scaffold inicial de serviço gerado a partir do contrato.</summary>
    ServiceScaffold = 4,

    /// <summary>Documentação gerada a partir do contrato.</summary>
    Documentation = 5,

    /// <summary>Evidência regulatória para audit trail.</summary>
    RegulatoryEvidence = 6,

    /// <summary>Changelog gerado entre versões.</summary>
    Changelog = 7,

    /// <summary>Guia de migração entre versões.</summary>
    MigrationGuide = 8
}
