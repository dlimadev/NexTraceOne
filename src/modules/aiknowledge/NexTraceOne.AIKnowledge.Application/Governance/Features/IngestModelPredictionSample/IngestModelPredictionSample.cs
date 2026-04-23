using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.IngestModelPredictionSample;

/// <summary>
/// Feature: IngestModelPredictionSample — ingestão de amostras de predição de modelo de IA.
///
/// Regista amostras de predição produzidas em produção para análise posterior de drift
/// de input/output e qualidade de modelo. Suporta ingestão individual ou em batch.
///
/// Wave AT.1 — AI Model Quality &amp; Drift Governance (AIKnowledge Governance).
/// </summary>
public static class IngestModelPredictionSample
{
    // ── Command ────────────────────────────────────────────────────────────
    /// <summary>Command para ingestão de uma amostra de predição de modelo.</summary>
    public sealed record Command(
        string TenantId,
        Guid ModelId,
        string ModelName,
        string ServiceId,
        DateTimeOffset PredictedAt,
        string? InputFeatureStatsJson,
        string? PredictedClass,
        double? ConfidenceScore,
        int? InferenceLatencyMs,
        string? ActualClass,
        bool IsFallback) : ICommand<Guid>;

    /// <summary>Validador do command <see cref="Command"/>.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ModelId).NotEqual(Guid.Empty);
            RuleFor(x => x.ModelName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ConfidenceScore)
                .InclusiveBetween(0.0, 1.0)
                .When(x => x.ConfidenceScore.HasValue);
            RuleFor(x => x.InferenceLatencyMs)
                .GreaterThanOrEqualTo(0)
                .When(x => x.InferenceLatencyMs.HasValue);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler do command <see cref="Command"/>.</summary>
    public sealed class Handler(
        IModelPredictionRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ModelName);
            Guard.Against.NullOrWhiteSpace(request.ServiceId);

            var sample = ModelPredictionSample.Create(
                modelId: request.ModelId,
                modelName: request.ModelName,
                serviceId: request.ServiceId,
                tenantId: request.TenantId,
                predictedAt: request.PredictedAt == default ? clock.UtcNow : request.PredictedAt,
                inputFeatureStatsJson: request.InputFeatureStatsJson,
                predictedClass: request.PredictedClass,
                confidenceScore: request.ConfidenceScore,
                inferenceLatencyMs: request.InferenceLatencyMs,
                actualClass: request.ActualClass,
                isFallback: request.IsFallback);

            await repository.AddAsync(sample, cancellationToken);
            return Result<Guid>.Success(sample.Id.Value);
        }
    }
}
