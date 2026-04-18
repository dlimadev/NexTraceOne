namespace NexTraceOne.Ingestion.Api.Security;

/// <summary>
/// Constantes de segurança da Ingestion API.
/// Centraliza os nomes de política, permissões obrigatórias e nomes de cabeçalhos
/// usados em toda a camada de autenticação e autorização desta API.
/// </summary>
internal static class IngestionApiSecurity
{
    /// <summary>Nome da política de autorização por API Key.</summary>
    internal const string PolicyName = "IngestionApiKeyWrite";

    /// <summary>Permissão mínima exigida em qualquer API Key válida.</summary>
    internal const string RequiredPermission = "integrations:write";

    /// <summary>Nome do cabeçalho de correlação propagado em todas as respostas.</summary>
    internal const string CorrelationHeaderName = "X-Correlation-Id";
}
