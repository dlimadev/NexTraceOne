using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.GetExternalAIUsage;

/// <summary>
/// Feature: GetExternalAIUsage — consolida métricas reais de uso de IA externa para
/// governança, visibilidade e controle operacional. Agrega dados de consultas e captures.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetExternalAIUsage
{
    // ── QUERY ─────────────────────────────────────────────────────────────

    /// <summary>Query para obter métricas agregadas de uso de IA externa.</summary>
    public sealed record Query(
        DateTimeOffset? From,
        DateTimeOffset? To) : IQuery<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To)
                .When(x => x.From.HasValue && x.To.HasValue)
                .WithMessage("'From' must be earlier than 'To'.");
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IKnowledgeCaptureRepository captureRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var metrics = await captureRepository.GetUsageMetricsAsync(
                request.From, request.To, cancellationToken);

            var approvalRate = metrics.TotalCaptures > 0
                ? Math.Round((double)metrics.ApprovedCaptures / metrics.TotalCaptures * 100, 2)
                : 0d;

            var reuseRate = metrics.ApprovedCaptures > 0
                ? Math.Round((double)metrics.TotalReuses / metrics.ApprovedCaptures * 100, 2)
                : 0d;

            var byProvider = metrics.ByProvider.Select(p => new ProviderUsage(
                p.ProviderId, p.ConsultationCount, p.TokensUsed)).ToList();

            return new Response(
                metrics.TotalConsultations,
                metrics.CompletedConsultations,
                metrics.FailedConsultations,
                metrics.TotalTokensUsed,
                byProvider,
                metrics.TotalCaptures,
                metrics.ApprovedCaptures,
                metrics.RejectedCaptures,
                metrics.PendingCaptures,
                metrics.TotalReuses,
                approvalRate,
                reuseRate,
                request.From,
                request.To);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Métricas agregadas de uso de IA externa.</summary>
    public sealed record Response(
        int TotalConsultations,
        int CompletedConsultations,
        int FailedConsultations,
        long TotalTokensUsed,
        IReadOnlyList<ProviderUsage> ByProvider,
        int TotalCaptures,
        int ApprovedCaptures,
        int RejectedCaptures,
        int PendingCaptures,
        long TotalReuses,
        double ApprovalRatePct,
        double ReuseRatePct,
        DateTimeOffset? PeriodFrom,
        DateTimeOffset? PeriodTo);

    /// <summary>Uso agregado por provedor de IA.</summary>
    public sealed record ProviderUsage(
        string ProviderId,
        int ConsultationCount,
        long TokensUsed);
}
