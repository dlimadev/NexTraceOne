using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Sessão de consulta de AI pair programming governado a partir de extensões IDE
/// (VS Code / Visual Studio). Rastreia query, resposta, tokens, governança e auditoria.
/// </summary>
public sealed class IdeQuerySession : AuditableEntity<IdeQuerySessionId>
{
    private IdeQuerySession() { }

    /// <summary>Identificador do developer que fez a consulta.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Cliente IDE utilizado ("vscode" ou "visualstudio").</summary>
    public string IdeClient { get; private set; } = string.Empty;

    /// <summary>Versão do cliente IDE.</summary>
    public string IdeClientVersion { get; private set; } = string.Empty;

    /// <summary>Tipo de consulta realizada.</summary>
    public IdeQueryType QueryType { get; private set; }

    /// <summary>Texto da consulta / prompt enviado.</summary>
    public string QueryText { get; private set; } = string.Empty;

    /// <summary>Contexto da consulta em formato JSONB (ficheiro, projecto, etc.).</summary>
    public string? QueryContext { get; private set; }

    /// <summary>Texto da resposta gerada pelo modelo de IA.</summary>
    public string? ResponseText { get; private set; }

    /// <summary>Nome do modelo de IA utilizado.</summary>
    public string ModelUsed { get; private set; } = string.Empty;

    /// <summary>Total de tokens consumidos (prompt + completion).</summary>
    public int TokensUsed { get; private set; }

    /// <summary>Tokens consumidos pelo prompt.</summary>
    public int PromptTokens { get; private set; }

    /// <summary>Tokens consumidos pela resposta (completion).</summary>
    public int CompletionTokens { get; private set; }

    /// <summary>Estado atual da sessão de consulta.</summary>
    public IdeQuerySessionStatus Status { get; private set; }

    /// <summary>Resultado da verificação de governança em formato JSONB.</summary>
    public string? GovernanceCheckResult { get; private set; }

    /// <summary>Tempo de resposta em milissegundos.</summary>
    public long? ResponseTimeMs { get; private set; }

    /// <summary>Timestamp UTC de submissão da consulta.</summary>
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>Timestamp UTC de resposta, bloqueio ou falha.</summary>
    public DateTimeOffset? RespondedAt { get; private set; }

    /// <summary>Mensagem de erro em caso de falha.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Controlo de concorrência optimista.</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova sessão de consulta IDE com estado Processing.
    /// </summary>
    public static IdeQuerySession Create(
        string userId,
        string ideClient,
        string ideClientVersion,
        IdeQueryType queryType,
        string queryText,
        string? queryContext,
        string modelUsed,
        Guid tenantId,
        DateTimeOffset submittedAt)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(ideClient);
        Guard.Against.NullOrWhiteSpace(ideClientVersion);
        Guard.Against.EnumOutOfRange(queryType);
        Guard.Against.NullOrWhiteSpace(queryText);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.Default(tenantId);

        return new IdeQuerySession
        {
            Id = IdeQuerySessionId.New(),
            UserId = userId.Trim(),
            IdeClient = ideClient.Trim(),
            IdeClientVersion = ideClientVersion.Trim(),
            QueryType = queryType,
            QueryText = queryText.Trim(),
            QueryContext = queryContext,
            ModelUsed = modelUsed.Trim(),
            TokensUsed = 0,
            PromptTokens = 0,
            CompletionTokens = 0,
            Status = IdeQuerySessionStatus.Processing,
            SubmittedAt = submittedAt,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Regista a resposta do modelo de IA com métricas de consumo.
    /// </summary>
    public void Respond(
        string responseText,
        int tokensUsed,
        int promptTokens,
        int completionTokens,
        long responseTimeMs,
        DateTimeOffset respondedAt)
    {
        Guard.Against.NullOrWhiteSpace(responseText);
        Guard.Against.Negative(tokensUsed);
        Guard.Against.Negative(promptTokens);
        Guard.Against.Negative(completionTokens);
        Guard.Against.Negative(responseTimeMs);

        ResponseText = responseText.Trim();
        TokensUsed = tokensUsed;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        ResponseTimeMs = responseTimeMs;
        Status = IdeQuerySessionStatus.Responded;
        RespondedAt = respondedAt;
    }

    /// <summary>
    /// Marca a consulta como bloqueada por política de governança.
    /// </summary>
    public void Block(string governanceCheckResult, DateTimeOffset respondedAt)
    {
        Guard.Against.NullOrWhiteSpace(governanceCheckResult);

        GovernanceCheckResult = governanceCheckResult;
        Status = IdeQuerySessionStatus.Blocked;
        RespondedAt = respondedAt;
    }

    /// <summary>
    /// Marca a consulta como falhada por erro técnico.
    /// </summary>
    public void Fail(string errorMessage, DateTimeOffset respondedAt)
    {
        Guard.Against.NullOrWhiteSpace(errorMessage);

        ErrorMessage = errorMessage.Trim();
        Status = IdeQuerySessionStatus.Failed;
        RespondedAt = respondedAt;
    }
}

/// <summary>Identificador fortemente tipado de IdeQuerySession.</summary>
public sealed record IdeQuerySessionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static IdeQuerySessionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static IdeQuerySessionId From(Guid id) => new(id);
}
