using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Registo de auditoria de uma query executada por um agente autónomo.
/// Cada chamada à Agent API cria um AgentQueryRecord para rastreabilidade completa.
/// Prefixo de tabela: iam_
/// </summary>
public sealed class AgentQueryRecord : Entity<AgentQueryRecordId>
{
    private AgentQueryRecord() { }

    public Guid TenantId { get; private set; }
    public Guid TokenId { get; private set; }
    public string QueryType { get; private set; } = string.Empty;
    public string? QueryParametersJson { get; private set; }
    public int ResponseCode { get; private set; }
    public long DurationMs { get; private set; }
    public DateTimeOffset ExecutedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static AgentQueryRecord Create(
        Guid tenantId,
        Guid tokenId,
        string queryType,
        int responseCode,
        long durationMs,
        DateTimeOffset executedAt,
        string? queryParametersJson = null,
        string? errorMessage = null)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Default(tokenId);
        Guard.Against.NullOrWhiteSpace(queryType);
        Guard.Against.Negative(durationMs);

        return new AgentQueryRecord
        {
            Id = AgentQueryRecordId.New(),
            TenantId = tenantId,
            TokenId = tokenId,
            QueryType = queryType.Trim(),
            QueryParametersJson = queryParametersJson,
            ResponseCode = responseCode,
            DurationMs = durationMs,
            ExecutedAt = executedAt,
            ErrorMessage = errorMessage,
        };
    }
}
