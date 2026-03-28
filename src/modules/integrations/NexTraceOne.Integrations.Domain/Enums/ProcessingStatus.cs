namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Estado de processamento semântico do payload de uma execução de ingestão.
/// Separado de <see cref="ExecutionResult"/>, que reflete o ciclo de vida da execução.
/// </summary>
public enum ProcessingStatus
{
    /// <summary>Metadados registados — payload ainda não processado semanticamente.</summary>
    MetadataRecorded = 0,

    /// <summary>Payload processado com sucesso — campos semânticos extraídos.</summary>
    Processed = 1,

    /// <summary>Processamento falhou — payload não pôde ser interpretado.</summary>
    Failed = 2
}
