using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetEnvironmentDriftReport;

/// <summary>
/// Feature: GetEnvironmentDriftReport — obtém um relatório de drift pelo identificador.
/// Retorna todas as secções do relatório incluindo drifts por dimensão e recomendações.
/// </summary>
public static class GetEnvironmentDriftReport
{
    /// <summary>Query para obter relatório de drift por identificador.</summary>
    public sealed record Query(Guid ReportId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReportId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém o relatório de drift.</summary>
    public sealed class Handler(
        IEnvironmentDriftReportRepository reportRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var report = await reportRepository.GetByIdAsync(
                EnvironmentDriftReportId.From(request.ReportId),
                cancellationToken);

            if (report is null)
                return RuntimeIntelligenceErrors.DriftReportNotFound(request.ReportId.ToString());

            return Result<Response>.Success(new Response(
                report.Id.Value,
                report.SourceEnvironment,
                report.TargetEnvironment,
                report.AnalyzedDimensions,
                report.ServiceVersionDrifts,
                report.ConfigurationDrifts,
                report.ContractVersionDrifts,
                report.DependencyDrifts,
                report.PolicyDrifts,
                report.Recommendations,
                report.TotalDriftItems,
                report.CriticalDriftItems,
                report.OverallSeverity.ToString(),
                report.Status.ToString(),
                report.GeneratedAt,
                report.ReviewedAt,
                report.ReviewComment));
        }
    }

    /// <summary>Resposta completa do relatório de drift.</summary>
    public sealed record Response(
        Guid ReportId,
        string SourceEnvironment,
        string TargetEnvironment,
        string AnalyzedDimensions,
        string? ServiceVersionDrifts,
        string? ConfigurationDrifts,
        string? ContractVersionDrifts,
        string? DependencyDrifts,
        string? PolicyDrifts,
        string? Recommendations,
        int TotalDriftItems,
        int CriticalDriftItems,
        string OverallSeverity,
        string Status,
        DateTimeOffset GeneratedAt,
        DateTimeOffset? ReviewedAt,
        string? ReviewComment);
}
