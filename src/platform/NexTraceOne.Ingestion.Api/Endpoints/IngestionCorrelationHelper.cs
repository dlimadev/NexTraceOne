using NexTraceOne.Ingestion.Api.Security;
using System.Diagnostics;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Utilitário de correlação de requests da Ingestion API.
/// Resolve o ID de correlação a partir da request ou gera um novo valor
/// quando nenhum ID externo é fornecido. Propaga o ID resolvido na resposta.
/// </summary>
internal static class IngestionCorrelationHelper
{
    /// <summary>
    /// Resolve o ID de correlação para um request de ingestão.
    /// Prioridade: <paramref name="requestCorrelationId"/> → cabeçalho X-Correlation-Id → Activity.Current.Id → TraceIdentifier.
    /// Propaga o ID resolvido no cabeçalho de resposta.
    /// </summary>
    internal static string ResolveCorrelationId(HttpContext httpContext, string? requestCorrelationId = null)
    {
        var correlationId = !string.IsNullOrWhiteSpace(requestCorrelationId)
            ? requestCorrelationId
            : httpContext.Request.Headers[IngestionApiSecurity.CorrelationHeaderName].FirstOrDefault();

        correlationId = string.IsNullOrWhiteSpace(correlationId)
            ? Activity.Current?.Id ?? httpContext.TraceIdentifier
            : correlationId;

        httpContext.Response.Headers[IngestionApiSecurity.CorrelationHeaderName] = correlationId;
        return correlationId;
    }
}
