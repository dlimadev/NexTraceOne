using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do serviço de validação e registo de quotas de tokens.
/// Consulta políticas de quota aplicáveis e o ledger de consumo para validar
/// se o utilizador/tenant pode executar a inferência pretendida.
/// </summary>
public sealed class AiTokenQuotaService : IAiTokenQuotaService
{
    private readonly IAiTokenQuotaPolicyRepository _policyRepository;
    private readonly IAiTokenUsageLedgerRepository _ledgerRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AiTokenQuotaService> _logger;

    public AiTokenQuotaService(
        IAiTokenQuotaPolicyRepository policyRepository,
        IAiTokenUsageLedgerRepository ledgerRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<AiTokenQuotaService> logger)
    {
        _policyRepository = policyRepository;
        _ledgerRepository = ledgerRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<TokenQuotaValidationResult> ValidateQuotaAsync(
        string userId,
        string tenantId,
        string providerId,
        string modelId,
        int estimatedTokens,
        CancellationToken ct = default)
    {
        var userPolicies = await _policyRepository.GetForUserAsync(userId, ct);
        var tenantPolicies = await _policyRepository.GetForTenantAsync(tenantId, ct);

        var allPolicies = userPolicies.Concat(tenantPolicies)
            .Where(p => p.IsEnabled)
            .Where(p => p.ProviderId is null || string.Equals(p.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
            .Where(p => p.ModelId is null || string.Equals(p.ModelId, modelId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (allPolicies.Count == 0)
        {
            _logger.LogDebug(
                "No active quota policies found for user {UserId}, tenant {TenantId} — allowing request",
                userId, tenantId);
            return new TokenQuotaValidationResult(true);
        }

        var now = _dateTimeProvider.UtcNow;
        var startOfDay = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var dailyUsage = await _ledgerRepository.GetTotalTokensForPeriodAsync(userId, startOfDay, now, ct);
        var monthlyUsage = await _ledgerRepository.GetTotalTokensForPeriodAsync(userId, startOfMonth, now, ct);

        foreach (var policy in allPolicies)
        {
            if (policy.MaxTotalTokensPerRequest > 0 && estimatedTokens > policy.MaxTotalTokensPerRequest)
            {
                _logger.LogWarning(
                    "Quota exceeded: request tokens {EstimatedTokens} > max per request {MaxPerRequest} for policy {PolicyName}",
                    estimatedTokens, policy.MaxTotalTokensPerRequest, policy.Name);

                return new TokenQuotaValidationResult(
                    false,
                    $"Estimated tokens ({estimatedTokens}) exceed max per request ({policy.MaxTotalTokensPerRequest})",
                    policy.Name,
                    policy.Id.Value);
            }

            if (policy.MaxTokensPerDay > 0 && dailyUsage + estimatedTokens > policy.MaxTokensPerDay)
            {
                _logger.LogWarning(
                    "Quota exceeded: daily usage {DailyUsage} + estimated {EstimatedTokens} > max daily {MaxDaily} for policy {PolicyName}",
                    dailyUsage, estimatedTokens, policy.MaxTokensPerDay, policy.Name);

                return new TokenQuotaValidationResult(
                    false,
                    $"Daily token limit ({policy.MaxTokensPerDay}) would be exceeded",
                    policy.Name,
                    policy.Id.Value);
            }

            if (policy.MaxTokensPerMonth > 0 && monthlyUsage + estimatedTokens > policy.MaxTokensPerMonth)
            {
                _logger.LogWarning(
                    "Quota exceeded: monthly usage {MonthlyUsage} + estimated {EstimatedTokens} > max monthly {MaxMonthly} for policy {PolicyName}",
                    monthlyUsage, estimatedTokens, policy.MaxTokensPerMonth, policy.Name);

                return new TokenQuotaValidationResult(
                    false,
                    $"Monthly token limit ({policy.MaxTokensPerMonth}) would be exceeded",
                    policy.Name,
                    policy.Id.Value);
            }
        }

        _logger.LogDebug(
            "Quota validation passed for user {UserId}, tenant {TenantId}, estimated tokens {EstimatedTokens}",
            userId, tenantId, estimatedTokens);

        return new TokenQuotaValidationResult(true);
    }

    public async Task RecordUsageAsync(
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
        CancellationToken ct = default)
    {
        var totalTokens = promptTokens + completionTokens;

        var entry = AiTokenUsageLedger.Record(
            userId: userId,
            tenantId: tenantId,
            providerId: providerId,
            modelId: modelId,
            modelName: modelName,
            promptTokens: promptTokens,
            completionTokens: completionTokens,
            totalTokens: totalTokens,
            policyId: null,
            policyName: null,
            isBlocked: false,
            blockReason: null,
            requestId: requestId,
            executionId: executionId,
            timestamp: _dateTimeProvider.UtcNow,
            status: status,
            durationMs: durationMs);

        await _ledgerRepository.AddAsync(entry, ct);

        _logger.LogInformation(
            "Recorded token usage: user {UserId}, provider {ProviderId}, model {ModelId}, total {TotalTokens}, request {RequestId}",
            userId, providerId, modelId, totalTokens, requestId);
    }
}
