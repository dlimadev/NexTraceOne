namespace NexTraceOne.BuildingBlocks.Observability.Ingestion;

/// <summary>
/// Opções de configuração para observabilidade do pipeline de ingestão.
/// Secção: "Ingestion:Metrics".
/// </summary>
public sealed class IngestionObservabilityOptions
{
    public const string SectionName = "Ingestion:Metrics";

    /// <summary>Activa a emissão de métricas de ingestão. Padrão: true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Taxa de amostragem para métricas de duração (0.0–1.0). Padrão: 1.0 (100%).</summary>
    public double SamplingRate { get; init; } = 1.0;
}
