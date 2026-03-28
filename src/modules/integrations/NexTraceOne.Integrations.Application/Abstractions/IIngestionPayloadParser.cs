namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Resultado do parsing semântico de um payload de ingestão.
/// </summary>
public sealed record IngestionParsedResult(
    string? ServiceName,
    string? Environment,
    string? Version,
    string? CommitSha,
    string? ChangeType,
    DateTimeOffset? Timestamp,
    Dictionary<string, string> AdditionalMetadata,
    bool IsSuccessful,
    string? ErrorMessage);

/// <summary>
/// Abstração para parsing semântico de payloads de ingestão de deploy/change.
/// </summary>
public interface IIngestionPayloadParser
{
    /// <summary>
    /// Tenta extrair campos semânticos de um payload JSON de deploy ou change.
    /// Nunca lança excepção — erros de parsing são retornados em <see cref="IngestionParsedResult.IsSuccessful"/>.
    /// </summary>
    /// <param name="rawPayload">JSON raw do payload recebido.</param>
    /// <returns>Resultado com campos extraídos e indicação de sucesso.</returns>
    IngestionParsedResult ParseDeployPayload(string rawPayload);
}
