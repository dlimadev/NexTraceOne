using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ClassifyPromptIntent;

/// <summary>
/// Feature: ClassifyPromptIntent — classifica a intenção de um prompt usando heurísticas de palavras-chave.
/// Retorna a intenção classificada, nível de confiança e recomendação de roteamento de modelo.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ClassifyPromptIntent
{
    /// <summary>Query de classificação de intenção de prompt.</summary>
    public sealed record Query(
        string Prompt,
        Guid? TenantId) : IQuery<Response>;

    /// <summary>Validador da query de classificação.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Prompt).NotEmpty().MaximumLength(8000);
        }
    }

    /// <summary>Handler que classifica a intenção e sugere o roteamento.</summary>
    public sealed class Handler(
        IPromptIntentClassifier classifier,
        IModelRoutingPolicyRepository policyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            Guard.Against.Null(request);

            var (intent, confidence) = classifier.Classify(request.Prompt);

            ModelRoutingRecommendation? recommendation = null;
            if (request.TenantId.HasValue)
            {
                var policy = await policyRepository.GetActiveAsync(
                    request.TenantId.Value, intent, ct);

                if (policy is not null)
                    recommendation = new ModelRoutingRecommendation(
                        PreferredModel: policy.PreferredModelName,
                        FallbackModel: policy.FallbackModelName,
                        MaxTokens: policy.MaxTokens,
                        MaxCostPerRequestUsd: policy.MaxCostPerRequestUsd);
            }

            return new Response(
                Intent: intent.ToString(),
                Confidence: confidence,
                Recommendation: recommendation);
        }
    }

    /// <summary>Recomendação de roteamento de modelo para a intenção classificada.</summary>
    public sealed record ModelRoutingRecommendation(
        string PreferredModel,
        string? FallbackModel,
        int MaxTokens,
        decimal MaxCostPerRequestUsd);

    /// <summary>Resposta da classificação de intenção de prompt.</summary>
    public sealed record Response(
        string Intent,
        double Confidence,
        ModelRoutingRecommendation? Recommendation);
}
