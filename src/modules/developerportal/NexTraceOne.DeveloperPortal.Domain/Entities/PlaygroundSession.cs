using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.DeveloperPortal.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma sessão de execução no playground interativo do portal.
/// Regista cada chamada de API feita pelo desenvolvedor no ambiente sandbox, incluindo request,
/// response, duração e cabeçalhos. Serve como trilha de auditoria para uso do playground e
/// como base para analytics de adoção do portal.
/// </summary>
public sealed class PlaygroundSession : AggregateRoot<PlaygroundSessionId>
{
    private PlaygroundSession() { }

    /// <summary>Identificador do ativo de API testado no playground.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome legível da API testada.</summary>
    public string ApiName { get; private set; } = string.Empty;

    /// <summary>Identificador do utilizador que executou a chamada.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Método HTTP utilizado na chamada (GET, POST, PUT, DELETE, etc.).</summary>
    public string HttpMethod { get; private set; } = string.Empty;

    /// <summary>Caminho do request enviado ao endpoint da API.</summary>
    public string RequestPath { get; private set; } = string.Empty;

    /// <summary>Corpo do request enviado, ou null para métodos sem body (GET, DELETE).</summary>
    public string? RequestBody { get; private set; }

    /// <summary>Cabeçalhos do request serializados como JSON string para persistência.</summary>
    public string? RequestHeaders { get; private set; }

    /// <summary>Código de status HTTP da resposta recebida.</summary>
    public int ResponseStatusCode { get; private set; }

    /// <summary>Corpo da resposta recebida, ou null se vazio.</summary>
    public string? ResponseBody { get; private set; }

    /// <summary>Duração total da execução em milissegundos.</summary>
    public long DurationMs { get; private set; }

    /// <summary>Ambiente de execução — sempre "sandbox" no playground.</summary>
    public string Environment { get; private set; } = "sandbox";

    /// <summary>Data/hora UTC em que a chamada foi executada.</summary>
    public DateTimeOffset ExecutedAt { get; private set; }

    /// <summary>
    /// Cria o registo de uma sessão de playground a partir dos dados de request/response.
    /// O ambiente é sempre "sandbox" — chamadas reais contra ambientes produtivos não são permitidas.
    /// </summary>
    public static PlaygroundSession Create(
        Guid apiAssetId,
        string apiName,
        Guid userId,
        string httpMethod,
        string requestPath,
        string? requestBody,
        string? requestHeaders,
        int responseStatusCode,
        string? responseBody,
        long durationMs,
        DateTimeOffset executedAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(apiName);
        Guard.Against.Default(userId);
        Guard.Against.NullOrWhiteSpace(httpMethod);
        Guard.Against.NullOrWhiteSpace(requestPath);

        return new PlaygroundSession
        {
            Id = PlaygroundSessionId.New(),
            ApiAssetId = apiAssetId,
            ApiName = apiName,
            UserId = userId,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            RequestBody = requestBody,
            RequestHeaders = requestHeaders,
            ResponseStatusCode = responseStatusCode,
            ResponseBody = responseBody,
            DurationMs = durationMs,
            Environment = "sandbox",
            ExecutedAt = executedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de PlaygroundSession.</summary>
public sealed record PlaygroundSessionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PlaygroundSessionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PlaygroundSessionId From(Guid id) => new(id);
}
