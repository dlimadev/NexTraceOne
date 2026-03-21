namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de artefacto gerado por um agent.
/// OpenApiDraft: rascunho de contrato OpenAPI/Swagger.
/// TestScenarios: cenários de teste gerados.
/// KafkaSchema: schema Avro para Kafka.
/// Documentation: documentação gerada.
/// Analysis: resultado de análise.
/// Generic: artefacto genérico.
/// </summary>
public enum AgentArtifactType
{
    Generic = 0,
    OpenApiDraft = 1,
    TestScenarios = 2,
    KafkaSchema = 3,
    Documentation = 4,
    Analysis = 5,
    CodeReview = 6,
    Checklist = 7,
}
