namespace NexTraceOne.Ingestion.Api.Security;

/// <summary>
/// Constantes de segurança da Ingestion API.
/// Centraliza os nomes de política, permissões obrigatórias e nomes de cabeçalhos
/// usados em toda a camada de autenticação e autorização desta API.
/// </summary>
internal static class IngestionApiSecurity
{
    /// <summary>Nome da política de autorização por API Key para operações de escrita (ingestão).</summary>
    internal const string PolicyName = "IngestionApiKeyWrite";

    /// <summary>Permissão mínima exigida em qualquer API Key de escrita válida.</summary>
    internal const string RequiredPermission = "integrations:write";

    /// <summary>
    /// Nome da política de autorização por API Key para operações de leitura (consulta).
    /// Usada por endpoints GET que expõem dados do NexTraceOne para consumo de sistemas externos.
    /// </summary>
    internal const string ReadPolicyName = "IngestionApiKeyRead";

    /// <summary>Permissão mínima exigida em qualquer API Key de leitura válida.</summary>
    internal const string RequiredReadPermission = "integrations:read";

    /// <summary>Nome do cabeçalho de correlação propagado em todas as respostas.</summary>
    internal const string CorrelationHeaderName = "X-Correlation-Id";
}
