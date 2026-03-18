using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Entrada imutável no ledger de consumo de tokens de IA.
/// Cada registo representa uma inferência individual e o respetivo consumo,
/// servindo como fonte de verdade para auditoria e FinOps.
///
/// Invariantes:
/// - Entidade é imutável após criação — sem métodos de alteração.
/// - UserId, TenantId, ProviderId e ModelId são obrigatórios.
/// - RequestId e ExecutionId são obrigatórios para correlação.
/// - Tokens não podem ser negativos.
/// </summary>
public sealed class AiTokenUsageLedger : AuditableEntity<AiTokenUsageLedgerId>
{
    private AiTokenUsageLedger() { }

    /// <summary>Identificador do utilizador que originou o pedido.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant do utilizador.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Identificador do provedor de IA utilizado.</summary>
    public string ProviderId { get; private set; } = string.Empty;

    /// <summary>Identificador do modelo utilizado.</summary>
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>Nome do modelo utilizado (desnormalizado para consulta rápida).</summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Número de tokens consumidos no prompt (entrada).</summary>
    public int PromptTokens { get; private set; }

    /// <summary>Número de tokens consumidos na resposta (saída).</summary>
    public int CompletionTokens { get; private set; }

    /// <summary>Total de tokens consumidos (prompt + completion).</summary>
    public int TotalTokens { get; private set; }

    /// <summary>Identificador da política de quota aplicada (null se sem política).</summary>
    public Guid? PolicyId { get; private set; }

    /// <summary>Nome da política de quota aplicada (desnormalizado).</summary>
    public string? PolicyName { get; private set; }

    /// <summary>Indica se o pedido foi bloqueado por quota.</summary>
    public bool IsBlocked { get; private set; }

    /// <summary>Razão do bloqueio (null se não bloqueado).</summary>
    public string? BlockReason { get; private set; }

    /// <summary>Identificador único do pedido HTTP/gRPC original.</summary>
    public string RequestId { get; private set; } = string.Empty;

    /// <summary>Identificador de execução para correlação com traces.</summary>
    public string ExecutionId { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o consumo foi registado.</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>Estado da inferência (ex: "Success", "Failed", "Timeout", "Blocked").</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Duração da inferência em milissegundos.</summary>
    public double DurationMs { get; private set; }

    /// <summary>
    /// Regista uma nova entrada no ledger de consumo de tokens.
    /// A entrada é imutável após criação — não existem métodos de alteração.
    /// </summary>
    public static AiTokenUsageLedger Record(
        string userId,
        string tenantId,
        string providerId,
        string modelId,
        string modelName,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        Guid? policyId,
        string? policyName,
        bool isBlocked,
        string? blockReason,
        string requestId,
        string executionId,
        DateTimeOffset timestamp,
        string status,
        double durationMs)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(providerId);
        Guard.Against.NullOrWhiteSpace(modelId);
        Guard.Against.NullOrWhiteSpace(modelName);
        Guard.Against.Negative(promptTokens);
        Guard.Against.Negative(completionTokens);
        Guard.Against.Negative(totalTokens);
        Guard.Against.NullOrWhiteSpace(requestId);
        Guard.Against.NullOrWhiteSpace(executionId);
        Guard.Against.NullOrWhiteSpace(status);
        Guard.Against.Negative(durationMs);

        return new AiTokenUsageLedger
        {
            Id = AiTokenUsageLedgerId.New(),
            UserId = userId,
            TenantId = tenantId,
            ProviderId = providerId,
            ModelId = modelId,
            ModelName = modelName,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens,
            PolicyId = policyId,
            PolicyName = policyName,
            IsBlocked = isBlocked,
            BlockReason = blockReason,
            RequestId = requestId,
            ExecutionId = executionId,
            Timestamp = timestamp,
            Status = status,
            DurationMs = durationMs
        };
    }
}

/// <summary>Identificador fortemente tipado de AiTokenUsageLedger.</summary>
public sealed record AiTokenUsageLedgerId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiTokenUsageLedgerId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiTokenUsageLedgerId From(Guid id) => new(id);
}
