namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Semântica da proveniência de uma relação no grafo de engenharia.
/// Distingue relações confirmadas manualmente de relações inferidas automaticamente,
/// permitindo ao usuário entender o nível de certeza e a origem do dado.
/// Essencial para explicabilidade e auditoria do grafo.
/// </summary>
public enum RelationshipSemantic
{
    /// <summary>Relação explícita — registrada manualmente por um engenheiro.</summary>
    Explicit = 0,

    /// <summary>Relação inferida — descoberta via OpenTelemetry, logs ou análise estática.</summary>
    Inferred = 1,

    /// <summary>Relação calculada — derivada de outros dados do grafo (ex: transitividade).</summary>
    Calculated = 2
}
