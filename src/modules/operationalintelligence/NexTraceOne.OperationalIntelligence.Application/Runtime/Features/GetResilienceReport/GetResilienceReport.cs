using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetResilienceReport;

/// <summary>
/// Feature: GetResilienceReport — obtém um relatório de resiliência pelo identificador.
/// Retorna todas as secções incluindo blast radius, telemetria, score e recomendações.
/// </summary>
public static class GetResilienceReport
{
    /// <summary>Query para obter relatório de resiliência por identificador.</summary>
    public sealed record Query(Guid ReportId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReportId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém o relatório de resiliência.</summary>
    public sealed class Handler(
        IResilienceReportRepository reportRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var report = await reportRepository.GetByIdAsync(
                ResilienceReportId.From(request.ReportId),
                cancellationToken);

            if (report is null)
                return RuntimeIntelligenceErrors.ResilienceReportNotFound(request.ReportId.ToString());

            return Result<Response>.Success(new Response(
                report.Id.Value,
                report.ChaosExperimentId,
                report.ServiceName,
                report.Environment,
                report.ExperimentType,
                report.ResilienceScore,
                report.TheoreticalBlastRadius,
                report.ActualBlastRadius,
                report.BlastRadiusDeviation,
                report.TelemetryObservations,
                report.LatencyImpactMs,
                report.ErrorRateImpact,
                report.RecoveryTimeSeconds,
                report.Strengths,
                report.Weaknesses,
                report.Recommendations,
                report.Status.ToString(),
                report.ReviewedByUserId,
                report.ReviewedAt,
                report.ReviewComment,
                report.GeneratedAt));
        }
    }

    /// <summary>Resposta completa do relatório de resiliência.</summary>
    public sealed record Response(
        Guid ReportId,
        Guid ChaosExperimentId,
        string ServiceName,
        string Environment,
        string ExperimentType,
        int ResilienceScore,
        string? TheoreticalBlastRadius,
        string? ActualBlastRadius,
        decimal? BlastRadiusDeviation,
        string? TelemetryObservations,
        decimal? LatencyImpactMs,
        decimal? ErrorRateImpact,
        int? RecoveryTimeSeconds,
        string? Strengths,
        string? Weaknesses,
        string? Recommendations,
        string Status,
        string? ReviewedByUserId,
        DateTimeOffset? ReviewedAt,
        string? ReviewComment,
        DateTimeOffset GeneratedAt);
}
