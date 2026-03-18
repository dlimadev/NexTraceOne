namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de validação e registo de quotas de tokens de IA.
/// Garante que o consumo respeita políticas de governança por utilizador, tenant, provider e modelo.
/// Integra com o Model Registry e o Token Usage Ledger.
/// </summary>
public interface IAiTokenQuotaService
{
    /// <summary>Valida se o consumo estimado está dentro da quota permitida.</summary>
    Task<TokenQuotaValidationResult> ValidateQuotaAsync(
        string userId,
        string tenantId,
        string providerId,
        string modelId,
        int estimatedTokens,
        CancellationToken ct = default);

    /// <summary>Regista o consumo efetivo de tokens no ledger de auditoria.</summary>
    Task RecordUsageAsync(
        string userId,
        string tenantId,
        string providerId,
        string modelId,
        string modelName,
        int promptTokens,
        int completionTokens,
        string requestId,
        string executionId,
        string status,
        double durationMs,
        CancellationToken ct = default);
}

/// <summary>Resultado da validação de quota de tokens.</summary>
public sealed record TokenQuotaValidationResult(
    bool IsAllowed,
    string? BlockReason = null,
    string? PolicyName = null,
    Guid? PolicyId = null);
