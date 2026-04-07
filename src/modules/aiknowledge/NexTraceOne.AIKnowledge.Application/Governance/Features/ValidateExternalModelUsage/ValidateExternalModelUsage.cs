using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ValidateExternalModelUsage;

/// <summary>
/// Feature: ValidateExternalModelUsage — valida se o uso de um modelo externo de IA é permitido.
/// Consulta:
///   - ai.external_models.require_approval: se true, modelos externos precisam de aprovação
///   - ai.data_classification.block_sensitive: se true, dados sensíveis não podem ser enviados a modelos externos
/// Pilar: AI Governance &amp; Developer Acceleration
/// </summary>
public static class ValidateExternalModelUsage
{
    /// <summary>Query para validar uso de modelo externo.</summary>
    public sealed record Query(
        string ModelName,
        string Provider,
        bool IsExternal,
        bool ContainsSensitiveData,
        bool HasApproval) : IQuery<Response>;

    /// <summary>Handler que avalia permissão de uso de modelo externo.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var gates = new List<GateResult>();

            if (!request.IsExternal)
            {
                return new Response(
                    ModelName: request.ModelName,
                    IsAllowed: true,
                    Gates: [],
                    Reason: "Internal model — no external governance restrictions apply",
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            // Gate 1: External model approval
            var requireApproval = await configService.ResolveEffectiveValueAsync(
                "ai.external_models.require_approval",
                ConfigurationScope.Tenant, null, cancellationToken);

            if (requireApproval?.EffectiveValue == "true")
            {
                gates.Add(new GateResult(
                    "ExternalModelApproval",
                    request.HasApproval,
                    request.HasApproval
                        ? "Model has required approval"
                        : "External model requires approval before use"));
            }

            // Gate 2: Sensitive data blocking
            var blockSensitive = await configService.ResolveEffectiveValueAsync(
                "ai.data_classification.block_sensitive",
                ConfigurationScope.Tenant, null, cancellationToken);

            if (blockSensitive?.EffectiveValue == "true" && request.ContainsSensitiveData)
            {
                gates.Add(new GateResult(
                    "SensitiveDataBlock",
                    false,
                    "Sensitive data cannot be sent to external AI models"));
            }
            else if (blockSensitive?.EffectiveValue == "true")
            {
                gates.Add(new GateResult(
                    "SensitiveDataBlock",
                    true,
                    "No sensitive data detected — external model allowed"));
            }

            var isAllowed = gates.Count == 0 || gates.All(g => g.Passed);

            return new Response(
                ModelName: request.ModelName,
                IsAllowed: isAllowed,
                Gates: gates,
                Reason: isAllowed
                    ? $"External model '{request.ModelName}' usage is allowed"
                    : $"External model '{request.ModelName}' is blocked by governance policy",
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resultado individual de um gate.</summary>
    public sealed record GateResult(string GateName, bool Passed, string Message);

    /// <summary>Resposta da validação de uso de modelo externo.</summary>
    public sealed record Response(
        string ModelName,
        bool IsAllowed,
        List<GateResult> Gates,
        string Reason,
        DateTimeOffset EvaluatedAt);
}
